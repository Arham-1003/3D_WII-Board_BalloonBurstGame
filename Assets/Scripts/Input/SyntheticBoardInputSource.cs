using BalloonBalance.Core;
using BalloonBalance.SignalProcessing;
using UnityEngine;

namespace BalloonBalance.Input
{
    public sealed class SyntheticBoardInputSource : MonoBehaviour, ICopInputSource
    {
        [SerializeField, Range(10f, 120f)] private float sampleRateHz = 50f;
        [SerializeField, Min(1f)] private float participantLoad = 70f;
        [SerializeField, Min(0f)] private float minimumTotalLoad = 10f;
        [SerializeField, Range(0f, 2f)] private float movementFrequencyHz = 0.12f;
        [SerializeField, Range(0f, 0.25f)] private float loadNoise = 0.01f;
        [SerializeField, Range(0f, 0.2f)] private float driftAmplitude = 0.015f;
        [SerializeField, Range(0f, 1f)] private float packetLossProbability;
        [SerializeField, Range(0f, 1f)] private float spikeProbability;
        [SerializeField, Range(0f, 0.5f)] private float spikeMagnitude = 0.2f;

        private CopSample latest;
        private long sequence;
        private double nextSampleTime;
        private bool hasSample;
        private System.Random random;

        public bool IsConnected => isActiveAndEnabled;

        private void OnEnable()
        {
            random = new System.Random(12345);
            sequence = 0;
            hasSample = false;
            nextSampleTime = Time.realtimeSinceStartupAsDouble;
        }

        private void Update()
        {
            // Unity can invoke Update during scene/test lifecycle transitions before a
            // newly selected source has received its normal OnEnable initialization.
            random ??= new System.Random(12345);
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextSampleTime) return;
            nextSampleTime = now + 1.0 / sampleRateHz;
            sequence++;
            if (random.NextDouble() < packetLossProbability) return;

            float t = (float)now;
            Vector2 target = new Vector2(Mathf.Sin(t * movementFrequencyHz * Mathf.PI * 2f) * 0.55f,
                Mathf.Sin(t * movementFrequencyHz * Mathf.PI) * 0.42f);
            target += Vector2.one * (Mathf.Sin(t * 0.07f) * driftAmplitude);
            if (random.NextDouble() < spikeProbability)
                target += Random.insideUnitCircle.normalized * spikeMagnitude;
            target = Vector2.ClampMagnitude(target, 0.9f);

            float fl = participantLoad * 0.25f * (1f - target.x) * (1f + target.y);
            float fr = participantLoad * 0.25f * (1f + target.x) * (1f + target.y);
            float rl = participantLoad * 0.25f * (1f - target.x) * (1f - target.y);
            float rr = participantLoad * 0.25f * (1f + target.x) * (1f - target.y);
            fl = Noisy(fl); fr = Noisy(fr); rl = Noisy(rl); rr = Noisy(rr);
            bool valid = CopCalculator.TryCalculate(fl, fr, rl, rr, minimumTotalLoad,
                out float total, out Vector2 cop, out CopValidityFlags flags);
            latest = new CopSample(sequence, now, now, fl, fr, rl, rr, total, cop, cop,
                CopSourceType.SyntheticBoard, flags,
                valid ? CopConnectionState.Connected : CopConnectionState.Faulted);
            hasSample = true;
        }

        public bool TryGetLatestSample(out CopSample sample) { sample = latest; return hasSample; }
        private float Noisy(float value) => Mathf.Max(0f, value * (1f + ((float)random.NextDouble() * 2f - 1f) * loadNoise));
    }
}
