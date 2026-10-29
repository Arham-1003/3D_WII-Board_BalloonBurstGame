using BalloonBalance.Core;

namespace BalloonBalance.Input
{
    public interface ICopInputSource
    {
        bool IsConnected { get; }
        bool TryGetLatestSample(out CopSample sample);
    }
}
