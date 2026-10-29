using UnityEngine;

namespace BalloonBalance.Calibration
{
    public static class CopMapper
    {
        public static Vector2 Map(Vector2 cop, CalibrationProfile profile)
        {
            Vector2 relative = cop - profile.neutral;
            float x = relative.x >= 0f ? relative.x / Mathf.Max(0.0001f, profile.rightRange)
                : relative.x / Mathf.Max(0.0001f, profile.leftRange);
            float y = relative.y >= 0f ? relative.y / Mathf.Max(0.0001f, profile.forwardRange)
                : relative.y / Mathf.Max(0.0001f, profile.backwardRange);
            x = RemapDeadZone(x, profile.deadZoneX);
            y = RemapDeadZone(y, profile.deadZoneY);
            if (profile.invertX) x = -x;
            if (profile.invertY) y = -y;
            return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
        }

        private static float RemapDeadZone(float value, float deadZone)
        {
            float magnitude = Mathf.Abs(value);
            if (magnitude <= deadZone) return 0f;
            return Mathf.Sign(value) * Mathf.Clamp01((magnitude - deadZone) / Mathf.Max(0.0001f, 1f - deadZone));
        }
    }
}
