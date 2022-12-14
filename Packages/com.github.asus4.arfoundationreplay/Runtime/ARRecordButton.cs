using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    [RequireComponent(typeof(Button))]
    public sealed class ARRecordButton : MonoBehaviour
    {
        [SerializeField]
        private bool _hideInReleaseBuild = true;

        [SerializeField]
        private Sprite _iconStart;

        [SerializeField]
        private Sprite _iconStop;

        private ARRecorder _recorder;
        private Button _button;

        private void Awake()
        {
            var origin = FindObjectOfType<XROrigin>();
            if (origin == null)
            {
                Debug.LogError("ARRecorder requires ARSessionOrigin in the scene");
                gameObject.SetActive(false);
                return;
            }

            _recorder = gameObject.AddComponent<ARRecorder>();
            bool needHidden = _hideInReleaseBuild && !Debug.isDebugBuild;
            gameObject.SetActive(!needHidden);
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
    }
}
