using System;
using System.Globalization;
using System.IO;
using System.Text;
using BalloonBalance.Input;
using UnityEngine;

namespace BalloonBalance.Logging
{
    public sealed class TrialSampleLogger : MonoBehaviour
    {
        [SerializeField] private CopInputManager inputManager;
        private StreamWriter writer;
        private string inProgressPath;
        private string activeSequenceMode;

        public string SavedPath { get; private set; }
        public int SampleCount { get; private set; }

        private void OnEnable()
        {
            if (inputManager != null) inputManager.SampleProcessed += Record;
        }

        public void BeginPracticeTrial(int seed, string sequenceMode = "Random")
        {
            Close(false);
            string directory = Path.Combine(Application.persistentDataPath, "TrialLogs");
            Directory.CreateDirectory(directory);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            activeSequenceMode = sequenceMode;
            string baseName = $"practice_{stamp}_{sequenceMode}_seed-{seed}";
            inProgressPath = Path.Combine(directory, baseName + ".samples.csv.inprogress");
            SavedPath = Path.Combine(directory, baseName + ".samples.csv");
            writer = new StreamWriter(inProgressPath, false, new UTF8Encoding(false));
            writer.WriteLine("sequence,source_seconds,receive_seconds,source,validity,connection,front_left,front_right,rear_left,rear_right,total_load,raw_x,raw_y,moving_average_x,moving_average_y,ema_x,ema_y,butterworth_x,butterworth_y,median_x,median_y,control_x,control_y,active_filter,sequence_mode,delta_seconds");
            SampleCount = 0;
        }

        public void CompleteTrial() => Close(true);

        private void Record(ProcessedCopSample processed)
        {
            if (writer == null) return;
            var sample = processed.Source;
            writer.WriteLine(string.Join(",",
                sample.SequenceNumber,
                F(sample.SourceTimestampSeconds), F(sample.ReceiveTimestampSeconds), sample.SourceType,
                sample.ValidityFlags, sample.ConnectionState,
                F(sample.FrontLeft), F(sample.FrontRight), F(sample.RearLeft), F(sample.RearRight), F(sample.TotalLoad),
                F(processed.Raw.x), F(processed.Raw.y), F(processed.MovingAverage.x), F(processed.MovingAverage.y),
                F(processed.Ema.x), F(processed.Ema.y), F(processed.Butterworth.x), F(processed.Butterworth.y),
                F(processed.Median.x), F(processed.Median.y), F(processed.Control.x), F(processed.Control.y),
                inputManager.ActiveFilter, activeSequenceMode, F(processed.DeltaTime)));
            SampleCount++;
            if (SampleCount % 50 == 0) writer.Flush();
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        private void Close(bool clean)
        {
            if (writer == null) return;
            writer.Flush();
            writer.Dispose();
            writer = null;
            if (clean && File.Exists(inProgressPath)) File.Move(inProgressPath, SavedPath);
        }

        private void OnDisable()
        {
            if (inputManager != null) inputManager.SampleProcessed -= Record;
            Close(false);
        }

        private void OnApplicationQuit() => Close(false);
    }
}
