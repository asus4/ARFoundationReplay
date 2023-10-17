namespace ARFoundationReplay
{
    /// <summary>
    /// Base interface for the AR recorder.
    /// </summary>
    internal interface IRecorder
    {
        bool IsRecording { get; }
        void StartRecording();
        void StopRecording();
    }
}
