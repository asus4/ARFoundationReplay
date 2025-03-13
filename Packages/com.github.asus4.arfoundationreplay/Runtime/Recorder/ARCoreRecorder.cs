using System;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;


#if UNITY_ANDROID
using UnityEngine.XR.ARCore;
#endif

namespace ARFoundationReplay
{
#if UNITY_ANDROID
    /// <summary>
    /// ARFoundation Recorder is not implemented yet.
    /// 
    /// This fallbacks 
    /// </summary>
    public class ARCoreRecorder : MonoBehaviour, IRecorder
    {
        [SerializeField]
        private ARSession _session = null;

        private ARCoreSessionSubsystem _subsystem;
        private string _recordingFilePath = string.Empty;

        public bool IsRecording => _subsystem.recordingStatus.Recording();

        private void Awake()
        {
            if (_session == null)
            {
                _session = FindFirstObjectByType<ARSession>();
            }
            if (_session == null)
            {
                Debug.LogError("ARRecorder requires ARSession in the scene");
                enabled = false;
                return;
            }

            _subsystem = _session.subsystem as ARCoreSessionSubsystem;
            Assert.IsNotNull(_subsystem);
        }

        public void StartRecording()
        {
            var session = _subsystem.session;
            if (session == null)
            {
                Debug.LogError("ARCore session is not ready");
                return;
            }
            if (IsRecording)
            {
                Debug.LogWarning("ARCore is already recording");
                return;
            }

            var rotation = Screen.orientation switch
            {
                ScreenOrientation.Portrait => 0,
                ScreenOrientation.LandscapeLeft => 90,
                ScreenOrientation.PortraitUpsideDown => 180,
                ScreenOrientation.LandscapeRight => 270,
                _ => 0
            };

            using var config = new ArRecordingConfig(session);
            _recordingFilePath = GetFilePath();
            config.SetMp4DatasetUri(session, $"file://{_recordingFilePath}");
            config.SetRecordingRotation(session, rotation);
            config.SetAutoStopOnPause(session, true);
            var status = _subsystem.StartRecording(config);
            Debug.Log($"StartRecording to {_recordingFilePath} => {status}");
        }

        public void StopRecording()
        {
            var session = _subsystem.session;
            if (session == null)
            {
                Debug.LogError("ARCore session is not ready");
                return;
            }
            if (!IsRecording)
            {
                Debug.LogWarning("ARCore is not recording");
                return;
            }

            var status = _subsystem.StopRecording();
            if (status != ArStatus.Success)
            {
                Debug.LogError($"StopRecording() failed: {status}");
                return;
            }
            else if (!File.Exists(_recordingFilePath))
            {
                Debug.LogError($"Recording completed, but no file was produced.");
            }

            Debug.Log($"ARCore session saved to {_recordingFilePath}");
            // ShareVideoWithAndroidIntent(_recordingFilePath);
        }

        private static string GetFilePath()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            string fileName = $"Record_{sceneName}_{DateTime.Now:MMdd_HHmm_ss}.mp4";
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        private static void ShareVideoWithAndroidIntent(string videoPath)
        {
            using var intent = new AndroidJavaObject("android.content.Intent");
            intent.Call<AndroidJavaObject>("setAction", "android.intent.action.SEND");
            intent.Call<AndroidJavaObject>("setType", "video/mp4");
            intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", GetUriForFile(videoPath));
            // intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.SUBJECT", "ARCore Recording");
            // intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", "ARCore Recording");
            // intent.Call<AndroidJavaObject>("addFlags", 1 << 31);
            using var chooser = intent.CallStatic<AndroidJavaObject>("createChooser", intent, "Share ARCore Recording");
            using var UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("startActivity", chooser);
        }

        private static AndroidJavaObject GetUriForFile(string videoPath)
        {
            // Uri.fromFile(new File("/sdcard/sample.jpg"))
            using var file = new AndroidJavaObject("java.io.File", videoPath);
            using var Uri = new AndroidJavaClass("android.net.Uri");
            return Uri.CallStatic<AndroidJavaObject>("fromFile", file);

            // using var file = new AndroidJavaObject("java.io.File", videoPath);
            // using var context = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            // using var provider = context.Call<AndroidJavaObject>("getPackageName") + ".fileprovider";
            // return new AndroidJavaClass("android.support.v4.content.FileProvider").CallStatic<AndroidJavaObject>("getUriForFile", context, provider, file);
        }
    }
#else
    /// <summary>
    /// ARFoundation Recorder is not implemented except on Android.
    /// </summary>
    public class ARCoreRecorder : MonoBehaviour, IRecorder
    {
        public bool IsRecording => false;

        public void StartRecording()
        {
            throw new NotImplementedException();
        }

        public void StopRecording()
        {
            throw new NotImplementedException();
        }
    }
#endif // UNITY_ANDROID
}
