using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    /// <summary>
    /// An utility button to start/stop AR recording.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ARRecordButton : MonoBehaviour
    {
        [SerializeField]
        private ARKitRecorder.Options _options = new()
        {
            width = 1920,
            height = 1080,
            targetFrameRate = 60,
        };

        [SerializeField]
        [Tooltip("Hide this button in release build")]
        private bool _hideInReleaseBuild = true;

        [SerializeField]
        private Sprite _iconStart;

        [SerializeField]
        private Sprite _iconStop;

        private IRecorder _recorder;
        private Button _button;

        private void Awake()
        {
            if (_hideInReleaseBuild && !Debug.isDebugBuild)
            {
                gameObject.SetActive(false);
                return;
            }

            var origin = FindObjectOfType<XROrigin>();
            if (origin == null)
            {
                Debug.LogError("ARRecorder requires ARSessionOrigin in the scene");
                gameObject.SetActive(false);
                return;
            }

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    _recorder = AddOrGetComponent<ARCoreRecorder>();
                    break;
                case RuntimePlatform.IPhonePlayer:
                    var recorder = AddOrGetComponent<ARKitRecorder>();
                    recorder.options = _options;
                    _recorder = recorder;
                    break;
                default:
                    Debug.LogWarning($"Recording is not supported on {Application.platform}");
                    return;
            }
        }

        private void OnDestroy()
        {
            _recorder = null;
        }

        private void OnEnable()
        {
            _button = GetComponent<Button>();
            _button.image.sprite = _iconStart;
            _button.onClick.AddListener(OnRecordButtonClicked);
        }

        private void OnDisable()
        {
            if (_button == null) { return; }
            _button.onClick.RemoveListener(OnRecordButtonClicked);
        }

        private void OnRecordButtonClicked()
        {
            if (_recorder == null)
            {
                return;
            }

            if (_recorder.IsRecording)
            {
                _recorder.StopRecording();
                _button.image.overrideSprite = null;
            }
            else
            {
                _recorder.StartRecording();
                _button.image.overrideSprite = _iconStop;
            }
        }

        private T AddOrGetComponent<T>() where T : Component
        {
            if (TryGetComponent<T>(out var component))
            {
                return component;
            }
            return gameObject.AddComponent<T>();
        }
    }
}
