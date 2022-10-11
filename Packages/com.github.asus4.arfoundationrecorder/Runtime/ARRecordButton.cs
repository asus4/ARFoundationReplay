using UnityEngine;
using UnityEngine.UI;

namespace ARFoundationRecorder
{
    [RequireComponent(typeof(Button))]
    public class ARRecordButton : MonoBehaviour
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
            bool needHidden = _hideInReleaseBuild && !Debug.isDebugBuild;
            if (needHidden)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_recorder == null)
            {
                _recorder = gameObject.AddComponent<ARRecorder>();
            }
            _button = GetComponent<Button>();
            _button.image.sprite = _iconStart;
            _button.onClick.AddListener(OnRecordButtonClicked);
        }

        private void OnDisable()
        {
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
