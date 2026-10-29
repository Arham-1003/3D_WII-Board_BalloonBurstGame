using BalloonBalance.Core;
using UnityEngine;

namespace BalloonBalance.SignalProcessing
{
    public static class CopCalculator
    {
        public static bool TryCalculate(float frontLeft, float frontRight, float rearLeft, float rearRight,
            float minimumTotalLoad, out float totalLoad, out Vector2 cop, out CopValidityFlags flags)
        {
            totalLoad = frontLeft + frontRight + rearLeft + rearRight;
            cop = Vector2.zero;
            flags = CopValidityFlags.None;
            if (!IsFinite(frontLeft) || !IsFinite(frontRight) || !IsFinite(rearLeft) || !IsFinite(rearRight))
            {
                flags = CopValidityFlags.InvalidNumber;
                return false;
            }
            if (frontLeft < 0f || frontRight < 0f || rearLeft < 0f || rearRight < 0f)
            {
                flags = CopValidityFlags.NegativeLoad;
                return false;
            }
            if (!IsFinite(totalLoad) || totalLoad < minimumTotalLoad)
            {
                flags = CopValidityFlags.LowTotalLoad;
                return false;
            }

            cop.x = ((frontRight + rearRight) - (frontLeft + rearLeft)) / totalLoad;
            cop.y = ((frontLeft + frontRight) - (rearLeft + rearRight)) / totalLoad;
            if (!IsFinite(cop.x) || !IsFinite(cop.y))
            {
                flags = CopValidityFlags.InvalidNumber;
                cop = Vector2.zero;
                return false;
            }
            flags = CopValidityFlags.Valid;
            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
