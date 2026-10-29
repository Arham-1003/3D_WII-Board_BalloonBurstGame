using BalloonBalance.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BalloonBalance.Input
{
    public sealed class KeyboardCopInputSource : MonoBehaviour, ICopInputSource
    {
        [SerializeField, Min(0.1f)] private float movementSpeed = 1.5f;

        private CopSample latestSample;
        private Vector2 currentPosition;
        private long sequenceNumber;
        private bool hasSample;

        public bool IsConnected => isActiveAndEnabled;

        private void OnEnable()
        {
            currentPosition = Vector2.zero;
            sequenceNumber = 0;
            hasSample = false;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Vector2 requested = keyboard == null ? Vector2.zero : ReadKeyboard(keyboard);
            currentPosition = Vector2.ClampMagnitude(
                currentPosition + requested * (movementSpeed * Time.unscaledDeltaTime),
                1f);

            double timestamp = Time.realtimeSinceStartupAsDouble;
            latestSample = new CopSample(
                ++sequenceNumber,
                timestamp,
                timestamp,
                0f,
                0f,
                0f,
                0f,
                0f,
                currentPosition,
                currentPosition,
                CopSourceType.Keyboard,
                CopValidityFlags.Valid,
                CopConnectionState.Connected);
            hasSample = true;
        }

        public bool TryGetLatestSample(out CopSample sample)
        {
            sample = latestSample;
            return hasSample && IsConnected;
        }

        private static Vector2 ReadKeyboard(Keyboard keyboard)
        {
            float horizontal = ReadAxis(
                keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed,
                keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed);
            float vertical = ReadAxis(
                keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed,
                keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed);
            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        private static float ReadAxis(bool negative, bool positive)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
