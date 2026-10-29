using UnityEngine;

namespace BalloonBalance.Core
{
    public readonly struct CopSample
    {
        public CopSample(
            long sequenceNumber,
            double sourceTimestampSeconds,
            double receiveTimestampSeconds,
            float frontLeft,
            float frontRight,
            float rearLeft,
            float rearRight,
            float totalLoad,
            Vector2 rawCop,
            Vector2 filteredCop,
            CopSourceType sourceType,
            CopValidityFlags validityFlags,
            CopConnectionState connectionState)
        {
            SequenceNumber = sequenceNumber;
            SourceTimestampSeconds = sourceTimestampSeconds;
            ReceiveTimestampSeconds = receiveTimestampSeconds;
            FrontLeft = frontLeft;
            FrontRight = frontRight;
            RearLeft = rearLeft;
            RearRight = rearRight;
            TotalLoad = totalLoad;
            RawCop = rawCop;
            FilteredCop = filteredCop;
            SourceType = sourceType;
            ValidityFlags = validityFlags;
            ConnectionState = connectionState;
        }

        public long SequenceNumber { get; }
        public double SourceTimestampSeconds { get; }
        public double ReceiveTimestampSeconds { get; }
        public float FrontLeft { get; }
        public float FrontRight { get; }
        public float RearLeft { get; }
        public float RearRight { get; }
        public float TotalLoad { get; }
        public Vector2 RawCop { get; }
        public Vector2 FilteredCop { get; }
        public CopSourceType SourceType { get; }
        public CopValidityFlags ValidityFlags { get; }
        public CopConnectionState ConnectionState { get; }

        public bool IsValid =>
            (ValidityFlags & CopValidityFlags.Valid) != 0 &&
            ConnectionState == CopConnectionState.Connected;
    }
}
