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

            const string key = "NSPhotoLibraryAddUsageDescription";
            const string desc = "Adds recorded videos to the library.";

            if (!plist.root.values.ContainsKey(key))
            {
                plist.root.SetString(key, desc);
            }
            plist.WriteToFile(plistPath);
        }
    }

}

#endif
