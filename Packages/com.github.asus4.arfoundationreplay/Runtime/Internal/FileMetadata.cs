using System;
using UnityEngine;

namespace ARFoundationReplay
{
    [Serializable]
    internal sealed class FileMetadata
    {
        public const string KEY = "ARFoundationReplayFileMetadata";

        public string version;
        public string modelName;
        public int screenWidth;
        public int screenHeight;
        public string[] encoders;

        public string Serialize()
        {
            return JsonUtility.ToJson(this);
        }

        public static FileMetadata Deserialize(string json)
        {
            return JsonUtility.FromJson<FileMetadata>(json);
        }
    }
}
