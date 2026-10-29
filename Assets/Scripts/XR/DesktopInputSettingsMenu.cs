using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BalloonBalance.XR
{
    public sealed class DesktopInputSettingsMenu : MonoBehaviour
    {
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject closedPrompt;
        [SerializeField] private Button closeButton;

        private float timeScaleBeforeOpening = 1f;
        private bool pausedByThisMenu;

        public static bool IsAnyMenuOpen { get; private set; }
        public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;

        private void Awake()
        {
#if UNITY_EDITOR
            if (settingsPanel == null || closedPrompt == null || closeButton == null)
            {
                enabled = false;
                return;
            }

            settingsPanel.SetActive(false);
            closedPrompt.SetActive(true);
            closeButton.onClick.AddListener(Close);
#else
            gameObject.SetActive(false);
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            bool keyboardToggle = keyboard != null && keyboard.yKey.wasPressedThisFrame;
            bool gamepadToggle = gamepad != null && gamepad.buttonNorth.wasPressedThisFrame;
            if (keyboardToggle || gamepadToggle) SetOpen(!IsOpen);
#endif
        }

        public void SetOpen(bool open)
        {
#if UNITY_EDITOR
            if (settingsPanel == null || closedPrompt == null || IsOpen == open) return;

            settingsPanel.SetActive(open);
            closedPrompt.SetActive(!open);
            IsAnyMenuOpen = open;
            if (open)
            {
                timeScaleBeforeOpening = Time.timeScale;
                Time.timeScale = 0f;
                pausedByThisMenu = true;
            }
            else
            {
                RestoreTimeScale();
            }
#endif
        }

        public void Close() => SetOpen(false);

        private void OnDisable()
        {
#if UNITY_EDITOR
            IsAnyMenuOpen = false;
            RestoreTimeScale();
#endif
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
#endif
        }

        private void RestoreTimeScale()
        {
            if (!pausedByThisMenu) return;
            Time.timeScale = timeScaleBeforeOpening;
            pausedByThisMenu = false;
        }
    }
}
