using UnityEngine;
using UnityEngine.UI;

namespace BalloonBalance.XR
{
    public sealed class DesktopSimulatorHeadLookToggle : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private EditorKeyboardCameraIsolation cameraIsolation;

        private void Awake()
        {
#if UNITY_EDITOR
            if (toggle == null || cameraIsolation == null)
            {
                enabled = false;
                return;
            }

            toggle.SetIsOnWithoutNotify(cameraIsolation.SimulatorHeadLookEnabled);
            toggle.onValueChanged.AddListener(cameraIsolation.SetSimulatorHeadLookEnabled);
#else
            gameObject.SetActive(false);
#endif
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (toggle != null && cameraIsolation != null)
            {
                toggle.onValueChanged.RemoveListener(cameraIsolation.SetSimulatorHeadLookEnabled);
            }
#endif
        }
    }
}
