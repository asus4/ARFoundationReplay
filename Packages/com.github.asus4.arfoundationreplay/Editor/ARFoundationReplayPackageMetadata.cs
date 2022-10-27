using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;

namespace ARFoundationReplay
{
    class XRPackage : IXRPackage
    {
        class ARRecorderLoaderMetadata : IXRLoaderMetadata
        {
            public string loaderName { get; set; }
            public string loaderType { get; set; }
            public List<BuildTargetGroup> supportedBuildTargets { get; set; }
        }

        class ARFoundationReplayPackageMetadata : IXRPackageMetadata
        {
            public string packageName { get; set; }
            public string packageId { get; set; }
            public string settingsType { get; set; }
            public List<IXRLoaderMetadata> loaderMetadata { get; set; }
        }


        private static IXRPackageMetadata s_Metadata = new ARFoundationReplayPackageMetadata()
        {
            packageName = "AR Foundation Replay",
            packageId = "com.github.asus4.arfoundationreplay",
            settingsType = typeof(ARFoundationReplaySettings).FullName,
            loaderMetadata = new List<IXRLoaderMetadata>()
            {
                new ARRecorderLoaderMetadata()
                {
                    loaderName = "AR Foundation Replay",
                    loaderType = typeof(ARFoundationReplayLoader).FullName,
                    supportedBuildTargets = new List<BuildTargetGroup>()
                    {
                        BuildTargetGroup.Standalone,
                    }
                },
            }
        };

        public IXRPackageMetadata metadata => s_Metadata;

        public bool PopulateNewSettingsInstance(ScriptableObject obj)
        {
            if (obj is ARFoundationReplaySettings settings)
            {
                ARFoundationReplaySettings.currentSettings = settings;
                return true;
            }

            return false;
        }
    }
}
