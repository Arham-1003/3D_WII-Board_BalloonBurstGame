using BalloonBalance.Input;
using BalloonBalance.XR;
using UnityEngine;

namespace BalloonBalance.Gameplay
{
    public sealed class PointerController : MonoBehaviour
    {
        [SerializeField] private CopInputManager inputManager;
        [SerializeField] private Transform interactionPlaneCenter;
        [SerializeField] private Vector2 movementExtents = new Vector2(1.6f, 1f);

        private void Reset()
        {
            movementExtents = new Vector2(1.6f, 1f);
        }

        private void LateUpdate()
        {
            if (DesktopInputSettingsMenu.IsAnyMenuOpen) return;

            if (inputManager == null || interactionPlaneCenter == null)
            {
                return;
            }

            Vector2 control = inputManager.ControlPosition;
            transform.position = interactionPlaneCenter.position +
                                 interactionPlaneCenter.right * (control.x * movementExtents.x) +
                                 interactionPlaneCenter.up * (control.y * movementExtents.y);
            transform.rotation = interactionPlaneCenter.rotation;
        }
    }
}
