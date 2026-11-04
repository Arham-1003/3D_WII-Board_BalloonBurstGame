using System;
using BalloonBalance.Calibration;
using BalloonBalance.Core;
using BalloonBalance.SignalProcessing;
using UnityEngine;

namespace BalloonBalance.Input
{
    public sealed class CopInputManager : MonoBehaviour
    {
        public enum FilterSelection { Raw, MovingAverage, Ema, Butterworth, Median, AdaptiveLowPass, AlphaBeta }
        public enum CalibrationStep { Idle, Neutral, Left, Right, Forward, Backward, Complete }

        [Header("Sources")]
        [SerializeField] private CopInputMode inputMode = CopInputMode.Keyboard;
        [SerializeField] private MonoBehaviour keyboardSource;
        [SerializeField] private MonoBehaviour syntheticSource;
        [SerializeField] private MonoBehaviour csvSource;
        [SerializeField] private MonoBehaviour udpSource;
        [Header("Processing")]
        [SerializeField] private FilterSelection filterSelection = FilterSelection.Ema;
        [SerializeField, Min(0.1f)] private float cutoffHz = 2.5f;
        [SerializeField, Min(2f)] private float nominalSampleRateHz = 50f;
        [SerializeField, Range(1, 30)] private int movingAverageWindow = 5;
        [SerializeField, Range(1, 9)] private int medianWindow = 3;
        [SerializeField, Min(0.1f)] private float adaptiveMinimumCutoffHz = 1.5f;
        [SerializeField, Min(0f)] private float adaptiveSpeedCoefficient = 3f;
        [SerializeField, Range(0.01f, 1f)] private float alphaBetaAlpha = 0.85f;
        [SerializeField, Range(0f, 1f)] private float alphaBetaBeta = 0.05f;
        [SerializeField] private CalibrationProfile calibration = new CalibrationProfile();

        private ICopInputSource inputSource;
        private readonly ICopFilter[] filters = new ICopFilter[7];
        private double previousSourceTime;
        private long lastProcessedSequence = long.MinValue;

        public event Action<ProcessedCopSample> SampleProcessed;
        public bool HasValidSample { get; private set; }
        public CopSample LatestSample { get; private set; }
        public Vector2 ControlPosition { get; private set; }
        public Vector2 RawOutput { get; private set; }
        public Vector2 MovingAverageOutput { get; private set; }
        public Vector2 EmaOutput { get; private set; }
        public Vector2 ButterworthOutput { get; private set; }
        public Vector2 MedianOutput { get; private set; }
        public Vector2 AdaptiveLowPassOutput { get; private set; }
        public Vector2 AlphaBetaOutput { get; private set; }
        public CopInputMode InputMode => inputMode;
        public FilterSelection ActiveFilter => filterSelection;
        public string ConnectionStatus => inputSource == null ? "NOT CONFIGURED" : inputSource.IsConnected ? "CONNECTED" : "WAITING / STALE";
        public CalibrationStep CurrentCalibrationStep { get; private set; }
        public string CalibrationFeedback { get; private set; }
        public string CalibrationInstruction => CurrentCalibrationStep switch
        {
            CalibrationStep.Neutral => "Stand naturally in the centre, then press CAPTURE CENTRE.",
            CalibrationStep.Left => "Lean comfortably left, then press CAPTURE LEFT.",
            CalibrationStep.Right => "Lean comfortably right, then press CAPTURE RIGHT.",
            CalibrationStep.Forward => "Lean comfortably forward, then press CAPTURE FORWARD.",
            CalibrationStep.Backward => "Lean comfortably backward, then press CAPTURE BACKWARD.",
            CalibrationStep.Complete => "Calibration saved. The full play area now matches your comfortable lean.",
            _ => "Calibrate before a participant's measured rounds."
        };

        private void Awake()
        {
            calibration.UpgradeLegacyWideDefaults();
            BuildFilters();
            SelectInput(inputMode);
        }

        private void Update()
        {
            HasValidSample = false;
            if (inputSource == null || !inputSource.IsConnected)
            {
                ControlPosition = Vector2.zero;
                return;
            }

            bool processed = false;
            if (inputSource is IBufferedCopInputSource buffered)
            {
                while (buffered.TryDequeueSample(out CopSample queued)) processed |= ProcessOnce(queued);
            }
            else if (inputSource.TryGetLatestSample(out CopSample latest)) processed = ProcessOnce(latest);
            HasValidSample = (processed || lastProcessedSequence != long.MinValue) && LatestSample.IsValid;
        }

        public void SelectInput(int mode) => SelectInput((CopInputMode)Mathf.Clamp(mode, 0, 3));

        public void SelectFilter(int filter) => SelectFilter((FilterSelection)Mathf.Clamp(filter, 0, 6));

        public void SelectFilter(FilterSelection filter)
        {
            if (filterSelection == filter) return;
            filterSelection = filter;
            ResetProcessing();
        }

        public void BeginCalibration()
        {
            CurrentCalibrationStep = CalibrationStep.Neutral;
            CalibrationFeedback = "Calibration started. Hold each position steady before capturing.";
            ResetProcessing();
        }

        public bool CaptureCalibrationPoint()
        {
            if (CurrentCalibrationStep == CalibrationStep.Idle || CurrentCalibrationStep == CalibrationStep.Complete)
            {
                BeginCalibration();
                return true;
            }
            if (!TryGetCalibrationSample(out CopSample calibrationSample))
            {
                CalibrationFeedback = inputSource == null || !inputSource.IsConnected
                    ? "Cannot capture: the selected input is not connected or its packets are stale."
                    : "Cannot capture: waiting for a valid load sample. Stand fully on the board and try again.";
                return false;
            }

            Vector2 raw = calibrationSample.RawCop;
            const float minimumComfortableRange = 0.02f;
            switch (CurrentCalibrationStep)
            {
                case CalibrationStep.Neutral:
                    calibration.neutral = raw;
                    CurrentCalibrationStep = CalibrationStep.Left;
                    CalibrationFeedback = "Centre captured.";
                    break;
                case CalibrationStep.Left:
                    if (calibration.neutral.x - raw.x < minimumComfortableRange)
                    {
                        CalibrationFeedback = "Not captured: lean farther left and hold steady.";
                        return false;
                    }
                    calibration.leftRange = calibration.neutral.x - raw.x;
                    CurrentCalibrationStep = CalibrationStep.Right;
                    CalibrationFeedback = "Left limit captured.";
                    break;
                case CalibrationStep.Right:
                    if (raw.x - calibration.neutral.x < minimumComfortableRange)
                    {
                        CalibrationFeedback = "Not captured: lean farther right and hold steady.";
                        return false;
                    }
                    calibration.rightRange = raw.x - calibration.neutral.x;
                    CurrentCalibrationStep = CalibrationStep.Forward;
                    CalibrationFeedback = "Right limit captured.";
                    break;
                case CalibrationStep.Forward:
                    if (raw.y - calibration.neutral.y < minimumComfortableRange)
                    {
                        CalibrationFeedback = "Not captured: lean farther forward and hold steady.";
                        return false;
                    }
                    calibration.forwardRange = raw.y - calibration.neutral.y;
                    CurrentCalibrationStep = CalibrationStep.Backward;
                    CalibrationFeedback = "Forward limit captured.";
                    break;
                case CalibrationStep.Backward:
                    if (calibration.neutral.y - raw.y < minimumComfortableRange)
                    {
                        CalibrationFeedback = "Not captured: lean farther backward and hold steady.";
                        return false;
                    }
                    calibration.backwardRange = calibration.neutral.y - raw.y;
                    calibration.createdAtUtc = DateTime.UtcNow.ToString("O");
                    CurrentCalibrationStep = CalibrationStep.Complete;
                    CalibrationFeedback = "Calibration complete and saved for this session.";
                    ResetProcessing();
                    break;
            }
            return true;
        }

        private bool TryGetCalibrationSample(out CopSample sample)
        {
            sample = LatestSample;
            if (inputSource == null || !inputSource.IsConnected) return false;
            if (sample.IsValid && IsFinite(sample.RawCop)) return true;
            return inputSource.TryGetLatestSample(out sample) && sample.IsValid && IsFinite(sample.RawCop);
        }

        private static bool IsFinite(Vector2 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y);

        public void SelectInput(CopInputMode mode)
        {
            inputMode = mode;
            SetSourceEnabled(keyboardSource, mode == CopInputMode.Keyboard);
            SetSourceEnabled(syntheticSource, mode == CopInputMode.SyntheticBoard);
            SetSourceEnabled(csvSource, mode == CopInputMode.CsvPlayback);
            SetSourceEnabled(udpSource, mode == CopInputMode.LiveUdp);
            inputSource = SourceFor(mode) as ICopInputSource;
            CalibrationFeedback = string.Empty;
            ResetProcessing();
            if (inputSource == null) Debug.LogError($"No valid {mode} input source is assigned.", this);
        }

        public void ResetProcessing()
        {
            previousSourceTime = 0d;
            lastProcessedSequence = long.MinValue;
            HasValidSample = false;
            ControlPosition = Vector2.zero;
            foreach (ICopFilter filter in filters) filter?.Reset();
        }

        private bool ProcessOnce(CopSample sample)
        {
            if (sample.SequenceNumber == lastProcessedSequence) return false;
            lastProcessedSequence = sample.SequenceNumber;
            if (!sample.IsValid)
            {
                LatestSample = sample;
                SampleProcessed?.Invoke(new ProcessedCopSample(sample, RawOutput, MovingAverageOutput,
                    EmaOutput, ButterworthOutput, MedianOutput, AdaptiveLowPassOutput,
                    AlphaBetaOutput, ControlPosition, 0f));
                return false;
            }
            float deltaTime = previousSourceTime > 0d
                ? Mathf.Max(0.0001f, (float)(sample.SourceTimestampSeconds - previousSourceTime))
                : 1f / nominalSampleRateHz;
            previousSourceTime = sample.SourceTimestampSeconds;

            if (sample.SourceType == CopSourceType.Keyboard)
            {
                RawOutput = MovingAverageOutput = EmaOutput = ButterworthOutput = MedianOutput =
                    AdaptiveLowPassOutput = AlphaBetaOutput = sample.FilteredCop;
                ControlPosition = Vector2.ClampMagnitude(sample.FilteredCop, 1f);
            }
            else
            {
                RawOutput = sample.RawCop;
                MovingAverageOutput = EmaOutput = ButterworthOutput = MedianOutput =
                    AdaptiveLowPassOutput = AlphaBetaOutput =
                    new Vector2(float.NaN, float.NaN);
                Vector2 selected = filters[(int)filterSelection].Process(sample.RawCop, deltaTime);
                switch (filterSelection)
                {
                    case FilterSelection.MovingAverage: MovingAverageOutput = selected; break;
                    case FilterSelection.Ema: EmaOutput = selected; break;
                    case FilterSelection.Butterworth: ButterworthOutput = selected; break;
                    case FilterSelection.Median: MedianOutput = selected; break;
                    case FilterSelection.AdaptiveLowPass: AdaptiveLowPassOutput = selected; break;
                    case FilterSelection.AlphaBeta: AlphaBetaOutput = selected; break;
                    default: RawOutput = selected; break;
                }
                ControlPosition = CopMapper.Map(selected, calibration);
            }

            LatestSample = sample;
            SampleProcessed?.Invoke(new ProcessedCopSample(sample, RawOutput, MovingAverageOutput,
                EmaOutput, ButterworthOutput, MedianOutput, AdaptiveLowPassOutput,
                AlphaBetaOutput, ControlPosition, deltaTime));
            return true;
        }

        private void BuildFilters()
        {
            filters[0] = new PassThroughCopFilter();
            filters[1] = new MovingAverageCopFilter(movingAverageWindow);
            filters[2] = new EmaCopFilter(cutoffHz);
            filters[3] = new ButterworthCopFilter(cutoffHz, nominalSampleRateHz);
            filters[4] = new MedianCopFilter(medianWindow);
            filters[5] = new AdaptiveLowPassCopFilter(adaptiveMinimumCutoffHz, adaptiveSpeedCoefficient);
            filters[6] = new AlphaBetaCopFilter(alphaBetaAlpha, alphaBetaBeta);
        }

        private MonoBehaviour SourceFor(CopInputMode mode) => mode switch
        {
            CopInputMode.SyntheticBoard => syntheticSource,
            CopInputMode.CsvPlayback => csvSource,
            CopInputMode.LiveUdp => udpSource,
            _ => keyboardSource
        };

        private static void SetSourceEnabled(MonoBehaviour source, bool value)
        {
            if (source != null) source.enabled = value;
        }
    }

    public readonly struct ProcessedCopSample
    {
        public ProcessedCopSample(CopSample source, Vector2 raw, Vector2 movingAverage, Vector2 ema,
            Vector2 butterworth, Vector2 median, Vector2 adaptiveLowPass, Vector2 alphaBeta,
            Vector2 control, float deltaTime)
        {
            Source = source; Raw = raw; MovingAverage = movingAverage; Ema = ema;
            Butterworth = butterworth; Median = median; Control = control; DeltaTime = deltaTime;
            AdaptiveLowPass = adaptiveLowPass; AlphaBeta = alphaBeta;
        }
        public CopSample Source { get; }
        public Vector2 Raw { get; }
        public Vector2 MovingAverage { get; }
        public Vector2 Ema { get; }
        public Vector2 Butterworth { get; }
        public Vector2 Median { get; }
        public Vector2 AdaptiveLowPass { get; }
        public Vector2 AlphaBeta { get; }
        public Vector2 Control { get; }
        public float DeltaTime { get; }
    }
}
