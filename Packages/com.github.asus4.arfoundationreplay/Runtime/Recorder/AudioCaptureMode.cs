namespace ARFoundationReplay
{
    /// <summary>
    /// Which audio, if any, is recorded into the video file as an audio track.
    /// Values must stay in sync with AvfiAudioMode in Avfi.m.
    /// </summary>
    public enum AudioCaptureMode
    {
        /// <summary>No audio track.</summary>
        None = 0,

        /// <summary>
        /// The device microphone (ambient sound of the real session), captured by the native plugin.
        /// Asks for the microphone permission. iOS only.
        /// </summary>
        NativeMicrophone = 1,

        /// <summary>
        /// The final mix that Unity plays, tapped from the AudioListener.
        /// </summary>
        UnityAudioOutput = 2,
    }
}
