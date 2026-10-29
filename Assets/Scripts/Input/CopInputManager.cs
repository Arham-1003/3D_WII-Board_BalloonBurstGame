using BalloonBalance.Core;
using BalloonBalance.Calibration;
using BalloonBalance.SignalProcessing;
using UnityEngine;

namespace BalloonBalance.Input
{
    public sealed class CopInputManager : MonoBehaviour
    {
        private enum FilterSelection { Raw, MovingAverage, Ema, Butterworth }

        [SerializeField] private MonoBehaviour inputSourceBehaviour;
        [SerializeField] private FilterSelection filterSelection = FilterSelection.Ema;
        [SerializeField, Min(0.1f)] private float cutoffHz = 2.5f;
        [SerializeField, Min(2f)] private float nominalSampleRateHz = 50f;
        [SerializeField, Range(1, 30)] private int movingAverageWindow = 5;
        [SerializeField] private CalibrationProfile calibration = new CalibrationProfile();

        private ICopInputSource inputSource;
        private ICopFilter filter;
        private double previousSourceTime;

        public bool HasValidSample { get; private set; }
        public CopSample LatestSample { get; private set; }
        public Vector2 ControlPosition { get; private set; }

        private void Awake()
        {
            inputSource = inputSourceBehaviour as ICopInputSource;
            if (inputSource == null)
            {
                Debug.LogError(
                    $"{nameof(CopInputManager)} requires a component implementing {nameof(ICopInputSource)}.",
                    this);
                enabled = false;
            }
            filter = CreateFilter();
            filter.Reset();
        }

        private void Update()
        {
            HasValidSample = false;
            ControlPosition = Vector2.zero;
            if (!inputSource.IsConnected ||
                !inputSource.TryGetLatestSample(out CopSample sample) ||
                !sample.IsValid)
            {
                return;
            }

            LatestSample = sample;
            HasValidSample = true;
            if (sample.SourceType == CopSourceType.Keyboard)
            {
                ControlPosition = Vector2.ClampMagnitude(sample.FilteredCop, 1f);
                return;
            }

            float deltaTime = previousSourceTime > 0d
                ? Mathf.Max(0f, (float)(sample.SourceTimestampSeconds - previousSourceTime))
                : 1f / nominalSampleRateHz;
            previousSourceTime = sample.SourceTimestampSeconds;
            Vector2 filtered = filter.Process(sample.RawCop, deltaTime);
            ControlPosition = CopMapper.Map(filtered, calibration);
        }

        public void ResetProcessing()
        {
            previousSourceTime = 0d;
            filter?.Reset();
        }

        private ICopFilter CreateFilter()
        {
            switch (filterSelection)
            {
                case FilterSelection.MovingAverage: return new MovingAverageCopFilter(movingAverageWindow);
                case FilterSelection.Ema: return new EmaCopFilter(cutoffHz);
                case FilterSelection.Butterworth: return new ButterworthCopFilter(cutoffHz, nominalSampleRateHz);
                default: return new PassThroughCopFilter();
            }
        }
    }
}
