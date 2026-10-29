using System;

namespace BalloonBalance.Core
{
    [Flags]
    public enum CopValidityFlags
    {
        None = 0,
        Valid = 1 << 0,
        LowTotalLoad = 1 << 1,
        InvalidNumber = 1 << 2,
        Stale = 1 << 3,
        Disconnected = 1 << 4,
        Malformed = 1 << 5,
        DuplicateSequence = 1 << 6,
        OutOfOrderSequence = 1 << 7,
        NegativeLoad = 1 << 8
    }
}
