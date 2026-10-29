using UnityEngine;

namespace BalloonBalance.SignalProcessing
{
    public interface ICopFilter
    {
        string Name { get; }
        void Reset();
        Vector2 Process(Vector2 rawCop, float deltaTime);
    }
}
