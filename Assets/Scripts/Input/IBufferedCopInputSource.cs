using BalloonBalance.Core;

namespace BalloonBalance.Input
{
    public interface IBufferedCopInputSource : ICopInputSource
    {
        bool TryDequeueSample(out CopSample sample);
    }
}
