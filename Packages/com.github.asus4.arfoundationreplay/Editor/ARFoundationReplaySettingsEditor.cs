using System.IO;
using UnityEngine;
using UnityEditor;

namespace ARFoundationReplay
{
    [CustomEditor(typeof(ARFoundationReplaySettings))]
    public class ARFoundationReplaySettingsEditor : Editor
    {
        private SerializedProperty _recordPath;
        private SerializedProperty _enableAudio;

        private void OnEnable()
        {
            _recordPath = serializedObject.FindProperty("_recordPath");
            _enableAudio = serializedObject.FindProperty("_enableAudio");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_recordPath);
            if (GUILayout.Button("Load Video"))
            {
                string path = SelectVideoPath();
                if (!string.IsNullOrWhiteSpace(path))
                {
                    _recordPath.stringValue = path;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_enableAudio);
            serializedObject.ApplyModifiedProperties();
        }

        private static string SelectVideoPath()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Select the AR Record Video", "", new string[] { "Video files", "mp4,mov,MP4,MOV", "All files", "*" });
            if (string.IsNullOrWhiteSpace(path))
            {
                return "";
            }

            // Convert to relative path from project root
            return Path.GetRelativePath(ARFoundationReplaySettings.ProjectRootPath, path);
        }
    }
}
