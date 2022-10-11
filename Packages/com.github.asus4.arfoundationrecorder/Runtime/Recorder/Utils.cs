using UnityEngine;
using DateTime = System.DateTime;

namespace ARFoundationRecorder
{
    static internal class PathUtil
    {
        private static string TemporaryDirectoryPath
            => Application.platform == RuntimePlatform.IPhonePlayer
                ? Application.temporaryCachePath : ".";

        public static string GetTimestampedFilename()
            => $"Record_{DateTime.Now:MMdd_HHmm_ss}.mp4";

        public static string GetTemporaryFilePath()
            => TemporaryDirectoryPath + "/" + GetTimestampedFilename();
    }
}
