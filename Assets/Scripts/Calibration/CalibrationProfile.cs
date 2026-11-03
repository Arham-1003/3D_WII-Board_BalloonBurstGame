using System;
using UnityEngine;

namespace BalloonBalance.Calibration
{
    [Serializable]
    public sealed class CalibrationProfile
    {
        public Vector2 neutral;
        public float leftRange = 0.1f;
        public float rightRange = 0.1f;
        public float forwardRange = 0.1f;
        public float backwardRange = 0.1f;
        [Range(0f, 0.5f)] public float deadZoneX = 0.08f;
        [Range(0f, 0.5f)] public float deadZoneY = 0.08f;
        public bool invertX;
        public bool invertY;
        public float neutralStandardDeviation;
        public string createdAtUtc;

        public bool IsUsable(float minimumRange = 0.05f) =>
            leftRange >= minimumRange && rightRange >= minimumRange &&
            forwardRange >= minimumRange && backwardRange >= minimumRange;

        public void UpgradeLegacyWideDefaults()
        {
            if (!string.IsNullOrEmpty(createdAtUtc)) return;
            if (leftRange >= 0.3f && rightRange >= 0.3f &&
                forwardRange >= 0.25f && backwardRange >= 0.2f)
            {
                leftRange = rightRange = forwardRange = backwardRange = 0.1f;
            }
        }
    }
}
