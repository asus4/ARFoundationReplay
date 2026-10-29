using UnityEngine;

namespace ARFoundationReplay
{
    /// <summary>
    /// Taps the final audio mix from the AudioListener and forwards it to the native recorder.
    /// Attached to the AudioListener GameObject while recording.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class AudioOutputCapture : MonoBehaviour
    {
        /// <summary>
        /// The audio track is always stereo. Mono output is duplicated and
        /// multichannel output keeps its front left/right pair.
        /// </summary>
        public const int Channels = 2;

        private volatile bool _isCapturing;
        private float[] _stereo = new float[4096 * Channels];

        public void Begin()
        {
            _isCapturing = true;
        }

        public void End()
        {
            _isCapturing = false;
        }

        // Called on the audio thread. The data is passed through unchanged.
        private unsafe void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_isCapturing || channels <= 0 || data.Length < channels)
            {
                return;
            }
            int frames = data.Length / channels;

            if (channels == Channels)
            {
                fixed (float* ptr = data)
                {
                    Avfi.AppendAudio(ptr, (uint)frames, Channels);
                }
                return;
            }

            if (_stereo.Length < frames * Channels)
            {
                _stereo = new float[frames * Channels];
            }
            int right = channels > 1 ? 1 : 0;
            for (int i = 0; i < frames; i++)
            {
                _stereo[i * Channels] = data[i * channels];
                _stereo[i * Channels + 1] = data[i * channels + right];
            }
            fixed (float* ptr = _stereo)
            {
                Avfi.AppendAudio(ptr, (uint)frames, Channels);
            }
        }
    }
}
