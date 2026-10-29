using BalloonBalance.Input;
using UnityEngine;

#if UNITY_EDITOR
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
#endif

namespace BalloonBalance.XR
{
    [DefaultExecutionOrder(10000)]
    public sealed class EditorKeyboardCameraIsolation : MonoBehaviour
    {
        [SerializeField] private Vector3 desktopLocalPosition = new Vector3(0f, 1.65f, 0f);
        [SerializeField, Min(0.01f)] private float mouseLookSensitivity = 0.12f;
        [SerializeField, Range(10f, 85f)] private float maximumPitch = 70f;
        [SerializeField] private bool simulatorHeadLookEnabled;

        public bool SimulatorHeadLookEnabled => simulatorHeadLookEnabled;

#if UNITY_EDITOR
        private float yaw;
        private float pitch;
        private TrackedPoseDriver trackedPoseDriver;

        public void SetSimulatorHeadLookEnabled(bool enabled)
        {
            simulatorHeadLookEnabled = enabled;
            if (!enabled)
            {
                yaw = 0f;
                pitch = 0f;
                ApplyFixedPose();
            }
        }

        private void LateUpdate()
        {
            if (FindAnyObjectByType<KeyboardCopInputSource>() == null) return;

            XRInteractionSimulator simulator = XRInteractionSimulator.instance;
            if (simulator != null && simulator.enabled) simulator.enabled = false;

            trackedPoseDriver ??= GetComponent<TrackedPoseDriver>();
            if (trackedPoseDriver != null && trackedPoseDriver.enabled) trackedPoseDriver.enabled = false;

            transform.localPosition = desktopLocalPosition;
            if (!simulatorHeadLookEnabled)
            {
                transform.localRotation = Quaternion.identity;
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * mouseLookSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * mouseLookSensitivity, -maximumPitch, maximumPitch);
            }

            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void ApplyFixedPose()
        {
            transform.SetLocalPositionAndRotation(desktopLocalPosition, Quaternion.identity);
        }
#endif
    }
}
