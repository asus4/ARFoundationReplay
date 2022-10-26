using UnityEngine;
using UnityEngine.XR.Management;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ARRecorder
{
    /// <summary>
    /// Setting for ARRecorder.
    /// </summary>
    [System.Serializable]
    [XRConfigurationData("ARRecorder", "ARRecorder.ARRecorderSetting")]
    public sealed class ARRecorderSettings : ScriptableObject
    {
        [SerializeField]
        private string _recordPath;

        internal string GetRecordPath()
        {
            return _recordPath;
        }

#if UNITY_EDITOR
        public static ARRecorderSettings currentSettings
        {
            get
            {
                return EditorBuildSettings.TryGetConfigObject(k_SettingsKey, out ARRecorderSettings settings)
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

        private const string k_SettingsKey = "ARRecorder.ARRecorderSetting";
    }
}
