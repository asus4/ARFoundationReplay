#import <AVFoundation/AVFoundation.h>
#import <CoreMedia/CMMetadata.h>
#import <stdatomic.h>

#if TARGET_OS_IOS
#import <UIKit/UIKit.h>
#endif

#define kMETADATA_ID_RAW @"mdta/com.github.asus4.avfi.raw"
#define kTIMESCALE 240
#define kAUDIO_BITRATE_PER_CHANNEL 64000

// Keep in sync with AudioCaptureMode.cs
typedef enum {
    AvfiAudioModeNone = 0,
    AvfiAudioModeNativeMicrophone = 1,
    AvfiAudioModeUnityAudioOutput = 2,
} AvfiAudioMode;

extern bool Avfi_HasMicrophonePermission(void);

// Writer objects
static AVAssetWriter* _writer;
static AVAssetWriterInput* _writerVideoInput;
static AVAssetWriterInputPixelBufferAdaptor* _pixelBufferAdaptor;
static AVAssetWriterInput* _writerMetadataInput;
static AVAssetWriterInputMetadataAdaptor* _metadataAdaptor;

// Audio objects shared by both audio modes
static AvfiAudioMode _audioMode = AvfiAudioModeNone;
static AVAssetWriterInput* _writerAudioInput;
static dispatch_queue_t _audioQueue; // Serial queue: owns _writerAudioInput and the PCM state below
static atomic_bool _isRecording = false;
static atomic_uint _recordingGeneration = 0; // Bumped per recording so late producer blocks are dropped

// Native microphone (iOS only)
#if TARGET_OS_IOS
@interface AvfiAudioCaptureDelegate : NSObject <AVCaptureAudioDataOutputSampleBufferDelegate>
@end
static AVCaptureSession* _captureSession;
static AvfiAudioCaptureDelegate* _captureDelegate;
static CMTime _audioBase; // Capture clock time at the recording origin
static NSString* _savedAudioCategory;
static NSString* _savedAudioMode;
static AVAudioSessionCategoryOptions _savedAudioOptions;
#endif

// Unity audio output: PCM pushed from the Unity audio thread. Touched only on the audio queue.
static CMAudioFormatDescriptionRef _pcmFormat;
static int _pcmSampleRate;
static int _pcmChannels;
static int64_t _pcmSampleCount;

#pragma mark - Audio (shared)

static dispatch_queue_t AudioQueue(void)
{
    static dispatch_once_t once;
    dispatch_once(&once, ^{
        _audioQueue = dispatch_queue_create("com.github.asus4.avfi.audio", DISPATCH_QUEUE_SERIAL);
    });
    return _audioQueue;
}

static bool AddAudioInput(int sampleRate, int channels)
{
    NSDictionary* settings = @{
        AVFormatIDKey: @(kAudioFormatMPEG4AAC),
        AVSampleRateKey: @(sampleRate),
        AVNumberOfChannelsKey: @(channels),
        AVEncoderBitRatePerChannelKey: @(kAUDIO_BITRATE_PER_CHANNEL),
    };
    AVAssetWriterInput* input = [AVAssetWriterInput assetWriterInputWithMediaType:AVMediaTypeAudio
                                                                   outputSettings:settings];
    // Note: mediaTimeScale must not be set on an audio input.
    input.expectsMediaDataInRealTime = YES;

    if (![_writer canAddInput:input])
    {
        NSLog(@"Avfi: Can't add audio input (%d Hz, %d ch)", sampleRate, channels);
        return false;
    }
    [_writer addInput:input];
    _writerAudioInput = input;
    return true;
}

// Must run on the audio queue. _writerAudioInput is cleared on this queue at teardown,
// so anything queued before that still lands in the file and anything after is dropped.
static void AppendAudioSampleBuffer(CMSampleBufferRef sampleBuffer)
{
    if (_writerAudioInput == nil)
    {
        return;
    }
    if (!_writerAudioInput.isReadyForMoreMediaData)
    {
        NSLog(@"Avfi: Audio input is not ready, dropping samples");
        return;
    }
    if (![_writerAudioInput appendSampleBuffer:sampleBuffer])
    {
        NSLog(@"Avfi: Failed to append audio (%ld: %@)", (long)_writer.status, _writer.error);
    }
}

#pragma mark - Audio: native microphone (iOS)

#if TARGET_OS_IOS

@implementation AvfiAudioCaptureDelegate
- (void)captureOutput:(AVCaptureOutput*)output
didOutputSampleBuffer:(CMSampleBufferRef)sampleBuffer
       fromConnection:(AVCaptureConnection*)connection
{
    if (!atomic_load(&_isRecording))
    {
        return;
    }

    // Rebase the timestamp to the recording origin.
    // A single timing entry carries the per-sample duration and the PTS of the first sample.
    CMSampleTimingInfo timing;
    if (CMSampleBufferGetSampleTimingInfo(sampleBuffer, 0, &timing) != noErr)
    {
        return;
    }
    timing.presentationTimeStamp = CMTimeSubtract(timing.presentationTimeStamp, _audioBase);
    timing.decodeTimeStamp = kCMTimeInvalid;
    if (CMTimeCompare(timing.presentationTimeStamp, kCMTimeZero) < 0)
    {
        // Captured before the recording origin
        return;
    }

    CMSampleBufferRef rebased = NULL;
    OSStatus status = CMSampleBufferCreateCopyWithNewTiming(kCFAllocatorDefault, sampleBuffer, 1, &timing, &rebased);
    if (status != noErr || rebased == NULL)
    {
        return;
    }
    AppendAudioSampleBuffer(rebased);
    CFRelease(rebased);
}
@end

static void ConfigureAudioSessionForRecording(void)
{
    AVAudioSession* session = [AVAudioSession sharedInstance];
    _savedAudioCategory = session.category;
    _savedAudioMode = session.mode;
    _savedAudioOptions = session.categoryOptions;

    // PlayAndRecord routes app audio to the receiver by default:
    // keep it on the speaker, and keep mixing with other apps like Unity's default Ambient category.
    NSError* error = nil;
    [session setCategory:AVAudioSessionCategoryPlayAndRecord
                    mode:AVAudioSessionModeDefault
                 options:AVAudioSessionCategoryOptionDefaultToSpeaker
                       | AVAudioSessionCategoryOptionAllowBluetoothHFP
                       | AVAudioSessionCategoryOptionMixWithOthers
                   error:&error];
    if (error)
    {
        NSLog(@"Avfi: Failed to set audio session category (%@)", error);
    }
    error = nil;
    [session setActive:YES error:&error];
    if (error)
    {
        NSLog(@"Avfi: Failed to activate audio session (%@)", error);
    }
}

static void RestoreAudioSession(void)
{
    if (_savedAudioCategory == nil)
    {
        return;
    }
    NSError* error = nil;
    [[AVAudioSession sharedInstance] setCategory:_savedAudioCategory
                                            mode:_savedAudioMode
                                         options:_savedAudioOptions
                                           error:&error];
    if (error)
    {
        NSLog(@"Avfi: Failed to restore audio session category (%@)", error);
    }
    _savedAudioCategory = nil;
    _savedAudioMode = nil;
}

static void ReleaseMicrophoneCapture(void)
{
    _captureSession = nil;
    _captureDelegate = nil;
}

static bool SetupMicrophoneCapture(void)
{
    AVCaptureDevice* device = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeAudio];
    if (device == nil)
    {
        NSLog(@"Avfi: No audio capture device found");
        return false;
    }

    NSError* error = nil;
    AVCaptureDeviceInput* input = [AVCaptureDeviceInput deviceInputWithDevice:device error:&error];
    if (input == nil)
    {
        NSLog(@"Avfi: Failed to create audio capture input (%@)", error);
        return false;
    }

    AVCaptureSession* session = [[AVCaptureSession alloc] init];
    // The AVAudioSession is configured by ConfigureAudioSessionForRecording instead.
    session.automaticallyConfiguresApplicationAudioSession = NO;

    AVCaptureAudioDataOutput* output = [[AVCaptureAudioDataOutput alloc] init];
    AvfiAudioCaptureDelegate* delegate = [[AvfiAudioCaptureDelegate alloc] init];
    [output setSampleBufferDelegate:delegate queue:AudioQueue()];

    [session beginConfiguration];
    if (![session canAddInput:input] || ![session canAddOutput:output])
    {
        [session commitConfiguration];
        NSLog(@"Avfi: Can't configure the audio capture session");
        return false;
    }
    [session addInput:input];
    [session addOutput:output];
    [session commitConfiguration];

    _captureSession = session;
    _captureDelegate = delegate;
    return true;
}

static void StartMicrophoneCapture(void)
{
    ConfigureAudioSessionForRecording();
    [_captureSession startRunning]; // Blocks until the session is running

    // Sample the origin as late as possible: the C# side starts its clock right after this call returns.
    CMClockRef clock = CMClockGetHostTimeClock();
    if (@available(iOS 15.4, *))
    {
        if (_captureSession.synchronizationClock != NULL)
        {
            clock = _captureSession.synchronizationClock;
        }
    }
    _audioBase = CMClockGetTime(clock);
}

static void StopMicrophoneCapture(void)
{
    if (_captureSession != nil)
    {
        [_captureSession stopRunning]; // Blocks; no delegate callbacks are scheduled after this
        RestoreAudioSession();
    }
    ReleaseMicrophoneCapture();
}

#endif // TARGET_OS_IOS

#pragma mark - Audio: Unity audio output (PCM)

// Must run on the audio queue
static bool SetupPCMFormat(int sampleRate, int channels)
{
    AudioStreamBasicDescription asbd = {0};
    asbd.mSampleRate = sampleRate;
    asbd.mFormatID = kAudioFormatLinearPCM;
    asbd.mFormatFlags = kAudioFormatFlagsNativeFloatPacked; // float32, interleaved
    asbd.mChannelsPerFrame = channels;
    asbd.mBitsPerChannel = 32;
    asbd.mBytesPerFrame = sizeof(float) * channels;
    asbd.mFramesPerPacket = 1;
    asbd.mBytesPerPacket = asbd.mBytesPerFrame;

    OSStatus status = CMAudioFormatDescriptionCreate(kCFAllocatorDefault, &asbd, 0, NULL, 0, NULL, NULL, &_pcmFormat);
    if (status != noErr)
    {
        NSLog(@"Avfi: CMAudioFormatDescriptionCreate failed (%d)", (int)status);
        _pcmFormat = NULL;
        return false;
    }
    _pcmSampleRate = sampleRate;
    _pcmChannels = channels;
    _pcmSampleCount = 0;
    return true;
}

// Must run on the audio queue
static void ReleasePCMFormat(void)
{
    if (_pcmFormat != NULL)
    {
        CFRelease(_pcmFormat);
        _pcmFormat = NULL;
    }
}

// Called from the Unity audio thread. Never blocks, and only copies the caller's samples here:
// the PCM format, sample counter and writer input are touched on the audio queue alone, so a
// callback that overlaps Avfi_EndRecording can't use state that is being torn down.
extern void Avfi_AppendAudio(const float* interleaved, uint32_t frameCount, uint32_t channels)
{
    if (!atomic_load(&_isRecording) || frameCount == 0 || channels == 0)
    {
        return;
    }

    // Copy the samples: Unity reuses its buffer after the callback returns.
    size_t bytes = (size_t)frameCount * channels * sizeof(float);
    CMBlockBufferRef block = NULL;
    OSStatus status = CMBlockBufferCreateWithMemoryBlock(kCFAllocatorDefault, NULL, bytes, kCFAllocatorDefault,
                                                         NULL, 0, bytes, kCMBlockBufferAssureMemoryNowFlag, &block);
    if (status != kCMBlockBufferNoErr || block == NULL)
    {
        return;
    }
    status = CMBlockBufferReplaceDataBytes(interleaved, block, 0, bytes);
    if (status != kCMBlockBufferNoErr)
    {
        CFRelease(block);
        return;
    }

    unsigned generation = atomic_load(&_recordingGeneration);
    dispatch_async(AudioQueue(), ^{
        // Samples from a recording that has ended, or that don't match the track format, are dropped.
        if (generation == atomic_load(&_recordingGeneration) && _pcmFormat != NULL && (int)channels == _pcmChannels)
        {
            CMTime pts = CMTimeMake(_pcmSampleCount, _pcmSampleRate);
            CMSampleBufferRef sampleBuffer = NULL;
            OSStatus createStatus = CMAudioSampleBufferCreateReadyWithPacketDescriptions(
                kCFAllocatorDefault, block, _pcmFormat, frameCount, pts, NULL, &sampleBuffer);
            if (createStatus == noErr && sampleBuffer != NULL)
            {
                // Advance even if the writer drops the buffer, so the timeline stays continuous.
                _pcmSampleCount += frameCount;
                AppendAudioSampleBuffer(sampleBuffer);
                CFRelease(sampleBuffer);
            }
        }
        CFRelease(block);
    });
}

#pragma mark - Microphone permission

extern void Avfi_RequestMicrophonePermission(void)
{
#if TARGET_OS_IOS
    if ([AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeAudio] != AVAuthorizationStatusNotDetermined)
    {
        return;
    }
    [AVCaptureDevice requestAccessForMediaType:AVMediaTypeAudio completionHandler:^(BOOL granted) {
        NSLog(@"Avfi: Microphone permission %@", granted ? @"granted" : @"denied");
    }];
#endif
}

extern bool Avfi_HasMicrophonePermission(void)
{
#if TARGET_OS_IOS
    return [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeAudio] == AVAuthorizationStatusAuthorized;
#else
    return false;
#endif
}

#pragma mark - Recording

// Round to the nearest tick. CMTimeMakeWithSeconds truncates, which turns e.g. 7/60 s
// into 27/240 instead of 28/240 and puts a one-tick jitter on the frame grid.
static CMTime FrameTime(double seconds)
{
    return CMTimeMake((int64_t)llround(seconds * kTIMESCALE), kTIMESCALE);
}

// audioMode: see AvfiAudioMode. sampleRate/channels describe the audio track
// (and, for UnityAudioOutput, the PCM format pushed via Avfi_AppendAudio).
extern void Avfi_StartRecording(const char* filePath, int width, int height,
                                int audioMode, int sampleRate, int channels)
{
    if (_writer)
    {
        NSLog(@"Recording has already been initiated.");
        return;
    }

    // Asset writer setup
    NSURL* filePathURL =
      [NSURL fileURLWithPath:[NSString stringWithUTF8String:filePath]];

    NSError* err;
    _writer =
      [[AVAssetWriter alloc] initWithURL: filePathURL
                                fileType: AVFileTypeQuickTimeMovie
                                   error: &err];
    _writer.movieTimeScale = kTIMESCALE;

    if (err)
    {
        NSLog(@"Failed to initialize AVAssetWriter (%@)", err);
        return;
    }

    // Asset writer input setup
    NSDictionary* colorPropertySettings =
    @{
        AVVideoColorPrimariesKey: AVVideoColorPrimaries_ITU_R_709_2,
        AVVideoYCbCrMatrixKey: AVVideoTransferFunction_ITU_R_709_2,
        AVVideoTransferFunctionKey: AVVideoYCbCrMatrix_ITU_R_709_2,
    };
    NSDictionary* settings =
    @{
        AVVideoCodecKey: AVVideoCodecTypeH264,
        AVVideoWidthKey: @(width),
        AVVideoHeightKey: @(height),
        AVVideoColorPropertiesKey: colorPropertySettings,
    };

    _writerVideoInput = [AVAssetWriterInput assetWriterInputWithMediaType: AVMediaTypeVideo
                                                      outputSettings: settings];
    _writerVideoInput.expectsMediaDataInRealTime = true;
    _writerVideoInput.mediaTimeScale = kTIMESCALE;

    [_writer addInput:_writerVideoInput];

    // Pixel buffer adaptor setup
    NSDictionary* attribs = @{
        (NSString*)kCVPixelBufferPixelFormatTypeKey: @(kCVPixelFormatType_32BGRA),
        (NSString*)kCVPixelBufferWidthKey: @(width),
        (NSString*)kCVPixelBufferHeightKey: @(height),
    };

    _pixelBufferAdaptor = [AVAssetWriterInputPixelBufferAdaptor assetWriterInputPixelBufferAdaptorWithAssetWriterInput: _writerVideoInput
                                                                                      sourcePixelBufferAttributes: attribs];
    
    // Metadata adaptor setup
    CMFormatDescriptionRef metadataFormatDescription = NULL;
    NSArray *specs = @[
       @{(__bridge NSString *)kCMMetadataFormatDescriptionMetadataSpecificationKey_Identifier : kMETADATA_ID_RAW,
         (__bridge NSString *)kCMMetadataFormatDescriptionMetadataSpecificationKey_DataType : (__bridge NSString *)kCMMetadataBaseDataType_RawData},
    ];
    OSStatus metadataStatus = CMMetadataFormatDescriptionCreateWithMetadataSpecifications(kCFAllocatorDefault, kCMMetadataFormatType_Boxed, (__bridge CFArrayRef)specs, &metadataFormatDescription);
    if(metadataStatus) {
        NSLog(@"CMMetadataFormatDescriptionCreateWithMetadataSpecifications failed with error %d", (int)metadataStatus);
    }
    _writerMetadataInput = [AVAssetWriterInput assetWriterInputWithMediaType:AVMediaTypeMetadata
                                                              outputSettings:nil
                                                            sourceFormatHint:metadataFormatDescription];
    _metadataAdaptor = [AVAssetWriterInputMetadataAdaptor assetWriterInputMetadataAdaptorWithAssetWriterInput: _writerMetadataInput];
    _writerMetadataInput.expectsMediaDataInRealTime = YES;
    
    [_writerMetadataInput addTrackAssociationWithTrackOfInput:_writerVideoInput type:AVTrackAssociationTypeMetadataReferent];
    [_writer addInput:_writerMetadataInput];

    // Optional audio track setup
    _audioMode = AvfiAudioModeNone;
    atomic_fetch_add(&_recordingGeneration, 1);
    channels = MAX(1, MIN(2, channels)); // More than 2 channels would need AVChannelLayoutKey
    if (audioMode == AvfiAudioModeNativeMicrophone)
    {
#if TARGET_OS_IOS
        if (!Avfi_HasMicrophonePermission())
        {
            NSLog(@"Avfi: Microphone permission is not granted, recording without audio");
        }
        else if (SetupMicrophoneCapture())
        {
            if (AddAudioInput(sampleRate, channels))
            {
                _audioMode = AvfiAudioModeNativeMicrophone;
            }
            else
            {
                ReleaseMicrophoneCapture();
            }
        }
#else
        NSLog(@"Avfi: Native microphone capture is only supported on iOS");
#endif
    }
    else if (audioMode == AvfiAudioModeUnityAudioOutput)
    {
        __block bool formatReady = false;
        dispatch_sync(AudioQueue(), ^{
            formatReady = SetupPCMFormat(sampleRate, channels);
        });
        if (formatReady && AddAudioInput(sampleRate, channels))
        {
            _audioMode = AvfiAudioModeUnityAudioOutput;
        }
        else
        {
            dispatch_sync(AudioQueue(), ^{
                ReleasePCMFormat();
            });
        }
    }
    
    // Recording start
    if (![_writer startWriting])
    {
        NSLog(@"Failed to start (%ld: %@)", _writer.status, _writer.error);
        return;
    }

    [_writer startSessionAtSourceTime:kCMTimeZero];

#if TARGET_OS_IOS
    if (_audioMode == AvfiAudioModeNativeMicrophone)
    {
        StartMicrophoneCapture();
    }
#endif
    atomic_store(&_isRecording, true);
}

extern void Avfi_AppendFrame(
    const void* source, uint32_t size,
    const void* metadata, uint32_t metadataSize,
    double time)
{
    if (!_writer)
    {
        NSLog(@"Recording hasn't been initiated.");
        return;
    }

    if (!_writerVideoInput.isReadyForMoreMediaData || !_writerMetadataInput.isReadyForMoreMediaData)
    {
        NSLog(@"Writer is not ready.");
        return;
    }

    // Buffer allocation
    CVPixelBufferRef buffer;
    CVReturn ret = CVPixelBufferPoolCreatePixelBuffer
      (NULL, _pixelBufferAdaptor.pixelBufferPool, &buffer);

    if (ret != kCVReturnSuccess)
    {
        NSLog(@"Can't allocate a pixel buffer (%d)", ret);
        NSLog(@"%ld: %@", _writer.status, _writer.error);
        return;
    }

    // Buffer update
    CVPixelBufferLockBaseAddress(buffer, 0);

    void* pointer = CVPixelBufferGetBaseAddress(buffer);
    size_t buffer_size = CVPixelBufferGetDataSize(buffer);
    memcpy(pointer, source, MIN(size, buffer_size));

    // Buffer submission
    BOOL success = [_pixelBufferAdaptor appendPixelBuffer:buffer
                                withPresentationTime:FrameTime(time)];
    if (!success) {
        NSLog(@"Warning: Unable to write buffer to video");
    }

    CVPixelBufferUnlockBaseAddress(buffer, 0);
    CVPixelBufferRelease(buffer);

    if (metadataSize > 0)
    {
        // Metadata submission
        AVMutableMetadataItem* metadataItem = [AVMutableMetadataItem metadataItem];
        metadataItem.identifier = kMETADATA_ID_RAW;
        metadataItem.dataType = (__bridge NSString *)kCMMetadataBaseDataType_RawData;
        metadataItem.value = [NSData dataWithBytes:metadata length:metadataSize];

        CMTimeRange metadataTime = CMTimeRangeMake(FrameTime(time), kCMTimeInvalid);
        AVTimedMetadataGroup* metadataGroup = [[AVTimedMetadataGroup alloc] initWithItems:@[metadataItem]
                                                                                timeRange:metadataTime];
        [_metadataAdaptor appendTimedMetadataGroup:metadataGroup];
    }    
}

extern void Avfi_AddMetadata(const char* key, const char* value)
{
    if (!_writer)
    {
        NSLog(@"Recording hasn't been initiated.");
        return;
    }
    // Create metadata from JSON value
    AVMutableMetadataItem* metadataItem = [AVMutableMetadataItem metadataItem];
    metadataItem.identifier = [NSString stringWithUTF8String:key];
    metadataItem.dataType = (__bridge NSString *)kCMMetadataBaseDataType_JSON;
    metadataItem.value = [NSString stringWithUTF8String:value];

    _writer.metadata = [_writer.metadata arrayByAddingObject:metadataItem];
    NSLog(@"Set metadata");
}

extern void Avfi_EndRecording(bool isSave)
{
    if (!_writer)
    {
        NSLog(@"Recording hasn't been initiated.");
        return;
    }

    // Stop accepting audio first. The audio queue owns the audio input and the PCM state, so this
    // block runs after every append that was queued before now, and nothing queued later can reach them.
    atomic_store(&_isRecording, false);
#if TARGET_OS_IOS
    StopMicrophoneCapture();
#endif
    dispatch_sync(AudioQueue(), ^{
        if (_writerAudioInput != nil)
        {
            [_writerAudioInput markAsFinished];
            _writerAudioInput = nil;
        }
        ReleasePCMFormat();
    });
    _audioMode = AvfiAudioModeNone;

    [_writerVideoInput markAsFinished];
    [_writerMetadataInput markAsFinished];

    if (isSave)
    {
#if TARGET_OS_IOS
        NSString* path = _writer.outputURL.path;
        [_writer finishWritingWithCompletionHandler: ^{
            UISaveVideoAtPathToSavedPhotosAlbum(path, nil, nil, nil);
        }];
#else
        [_writer finishWritingWithCompletionHandler: ^{}];

#endif
    }

    _writer = NULL;
    _writerVideoInput = NULL;
    _pixelBufferAdaptor = NULL;
    _writerMetadataInput = NULL;
    _metadataAdaptor = NULL;
}
