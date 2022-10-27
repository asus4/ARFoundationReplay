using UnityEngine;
using UnityEngine.XR.Management;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ARFoundationReplay
{
    /// <summary>
    /// Setting for ARRecorder.
    /// </summary>
    [System.Serializable]
    [XRConfigurationData("AR Foundation Replay", k_SettingsKey)]
    public sealed class ARFoundationReplaySettings : ScriptableObject
    {
        [SerializeField]
        private string _recordPath;

        internal string GetRecordPath()
        {
            return _recordPath;
        }

#if UNITY_EDITOR
        public static ARFoundationReplaySettings currentSettings
        {
            get
            {
                return EditorBuildSettings.TryGetConfigObject(k_SettingsKey, out ARFoundationReplaySettings settings)
                    ? settings
                    : null;
            }
            set
            {
                if (value == null)
                {
                    EditorBuildSettings.RemoveConfigObject(k_SettingsKey);
                }
                else
                {
                    EditorBuildSettings.AddConfigObject(k_SettingsKey, value, true);
                }
            }
        }
#endif // UNITY_EDITOR

        private const string k_SettingsKey = "ARFoundationReplay.ARFoundationReplaySettings";
    }
}
