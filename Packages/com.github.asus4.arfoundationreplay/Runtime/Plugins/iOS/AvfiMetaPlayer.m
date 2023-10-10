#import <AVFoundation/AVFoundation.h>

@interface RawMetadata: NSObject
@property (nonatomic, assign) CMTime time;
@property (nonatomic, retain) NSData* data;
@end

@implementation RawMetadata
@end

static AVAssetReader* _reader;
static AVAssetReaderTrackOutput* _readerMetadataOutput;
static AVAssetReaderOutputMetadataAdaptor* _metadataAdaptor;
static NSMutableArray<RawMetadata*>* _metadataBuffer = nil;

#define kMETADATA_ID_RAW @"mdta/com.github.asus4.avfi.raw"
#define kTIMESCALE 240

extern void Avfi_UnloadMetadata(void);

extern bool Avfi_LoadMetadata(const char* filePath) {
    Avfi_UnloadMetadata();
    // Load AVAsset from file path
    NSURL* url = [NSURL fileURLWithPath:[NSString stringWithUTF8String:filePath]];
    NSLog(@"Avfi_LoadMetadata  (%@)", url);
    AVAsset* asset = [AVAsset assetWithURL:url];

    // Ensure the asset has at leaset one metadata track.
    NSArray* tracks = [asset tracksWithMediaType:AVMediaTypeMetadata];
    if (tracks.count == 0) {
        NSLog(@"No metadata track found in the video file: %@.", url);
        return false;
    }
    
    NSError* error;
    _reader = [AVAssetReader assetReaderWithAsset:asset error:&error];
    if (error) {
        NSLog(@"Failed to initialize AVAssetReader (%@)", error);
        return false;
    }

    // At the moment, we only support one metadata track.
    NSLog(@"Found %lu tracks", (unsigned long)tracks.count);
    AVAssetTrack* track = tracks[0];

    // Create a reader output for the metadata track.
    _readerMetadataOutput = [AVAssetReaderTrackOutput assetReaderTrackOutputWithTrack:track outputSettings:nil];
    [_reader addOutput:_readerMetadataOutput];

    // Create a metadata adaptor for the reader output
    _metadataAdaptor = [AVAssetReaderOutputMetadataAdaptor assetReaderOutputMetadataAdaptorWithAssetReaderTrackOutput:_readerMetadataOutput];
    
    _metadataBuffer = [[NSMutableArray alloc] init];
    [_reader startReading];

    // Read all metadata from the adaptor 
    AVTimedMetadataGroup* group;
    while(true) {
        group = [_metadataAdaptor nextTimedMetadataGroup];
        if(group == nil) {
            break;
        }

        CMTimeRange timeRange = group.timeRange;
        for(AVMetadataItem* item in group.items) {
            if ([item.identifier isEqualToString:kMETADATA_ID_RAW]) {
                RawMetadata* rawMetadata = [[RawMetadata alloc] init];
                rawMetadata.time = timeRange.start;
                rawMetadata.data = [NSData dataWithData:(NSData*) item.value];
                [_metadataBuffer addObject:rawMetadata];
            }
        }
    }

    [_reader cancelReading];

    NSLog(@"Avfi_LoadMetadata: %@, Found %lu metadata", url, (unsigned long)_metadataBuffer.count);
    return true;
}

extern void Avfi_UnloadMetadata(void)
{
    _reader = nil;
    _readerMetadataOutput = nil;
    _metadataAdaptor = nil;
    if(_metadataBuffer) {
        [_metadataBuffer removeAllObjects];
        _metadataBuffer = nil;
    }
}

extern uint32_t Avfi_GetBufferSize(void)
{
    if(_metadataBuffer==nil) {
        return 0;
    }

    // Find max byte size of all metadata
    uint32_t maxSize = 0;
    for(RawMetadata* rawMetadata in _metadataBuffer) {
        maxSize = MAX(maxSize, (uint32_t)rawMetadata.data.length);
    }
    return maxSize;
}

extern uint32_t Avfi_PeekMetadata(double time, void* data)
{
    if(_metadataBuffer == nil) {
        return 0;
    }

    for(RawMetadata* rawMetadata in _metadataBuffer) {
        if(CMTIME_COMPARE_INLINE(rawMetadata.time, >=, CMTimeMakeWithSeconds(time, kTIMESCALE))) {
            uint32_t length = (uint32_t)rawMetadata.data.length;
            memcpy(data, rawMetadata.data.bytes, rawMetadata.data.length);
            return length;
        }
    }
    return 0;
}
