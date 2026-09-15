#if UNITY_IOS

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

namespace ARFoundationReplay
{
    // Xcode project file modifier for iOS support
    public class PbxModifier
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget != BuildTarget.iOS) return;

            var plistPath = Path.Combine(path, "Info.plist");

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            AddUsageDescription(plist, "NSPhotoLibraryAddUsageDescription", "Adds recorded videos to the library.");
            // Required by AudioCaptureMode.NativeMicrophone
            AddUsageDescription(plist, "NSMicrophoneUsageDescription", "Records ambient audio with the AR session.");
            plist.WriteToFile(plistPath);
        }

        private static void AddUsageDescription(PlistDocument plist, string key, string description)
        {
            if (!plist.root.values.ContainsKey(key))
            {
                plist.root.SetString(key, description);
            }
        }
    }

}

#endif
