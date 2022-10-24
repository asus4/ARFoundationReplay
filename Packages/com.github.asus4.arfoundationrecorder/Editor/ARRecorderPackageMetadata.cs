using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;

namespace ARFoundationRecorder
{
    class XRPackage : IXRPackage
    {
        class ARRecorderLoaderMetadata : IXRLoaderMetadata
        {
            public string loaderName { get; set; }
            public string loaderType { get; set; }
            public List<BuildTargetGroup> supportedBuildTargets { get; set; }
        }

        class ARRecorderPackageMetadata : IXRPackageMetadata
        {
            public string packageName { get; set; }
            public string packageId { get; set; }
            public string settingsType { get; set; }
            public List<IXRLoaderMetadata> loaderMetadata { get; set; }
        }


        private static IXRPackageMetadata s_Metadata = new ARRecorderPackageMetadata()
        {
            packageName = "ARRecorder",
            packageId = "com.github.asus4.arfoundationrecorder",
            settingsType = typeof(ARRecorderSettings).FullName,
            loaderMetadata = new List<IXRLoaderMetadata>()
            {
                new ARRecorderLoaderMetadata()
                {
                    loaderName = "ARRecorder",
                    loaderType = typeof(ARRecorderLoader).FullName,
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
            if (obj is ARRecorderSettings settings)
            {
                ARRecorderSettings.currentSettings = settings;
                return true;
            }

            return false;
        }
    }
}
