using System;
using UnityEngine;

namespace BalloonBalance.Calibration
{
    [Serializable]
    public sealed class CalibrationProfile
    {
        public Vector2 neutral;
        public float leftRange = 0.35f;
        public float rightRange = 0.35f;
        public float forwardRange = 0.3f;
        public float backwardRange = 0.25f;
        [Range(0f, 0.5f)] public float deadZoneX = 0.08f;
        [Range(0f, 0.5f)] public float deadZoneY = 0.08f;
        public bool invertX;
        public bool invertY;
        public float neutralStandardDeviation;
        public string createdAtUtc;

        public bool IsUsable(float minimumRange = 0.05f) =>
            leftRange >= minimumRange && rightRange >= minimumRange &&
            forwardRange >= minimumRange && backwardRange >= minimumRange;
    }
}
