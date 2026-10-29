using UnityEngine;

namespace BalloonBalance.XR
{
    public sealed class EditorHeadHeightFallback : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float standingHeight = 1.65f;
        [SerializeField, Min(0f)] private float minimumValidTrackedHeight = 0.5f;

#if UNITY_EDITOR
        private void LateUpdate()
        {
            Vector3 localPosition = transform.localPosition;
            if (localPosition.y >= minimumValidTrackedHeight) return;

            localPosition.y = standingHeight;
            transform.localPosition = localPosition;
        }
#endif
    }
}
