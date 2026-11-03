using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace BalloonBalance.Logging
{
    public sealed class TrialEventLogger : MonoBehaviour
    {
        private readonly Stopwatch monotonicClock = new Stopwatch();
        private StreamWriter writer;
        private string inProgressPath;
        private string activeSequenceMode;

        public string SavedPath { get; private set; }
        public int EventCount { get; private set; }

        public void BeginPracticeTrial(int seed, string sequenceMode = "Random")
        {
            CloseTrial(false);
            string directory = Path.Combine(Application.persistentDataPath, "TrialLogs");
            Directory.CreateDirectory(directory);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            activeSequenceMode = sequenceMode;
            string baseName = $"practice_{stamp}_{sequenceMode}_seed-{seed}";
            inProgressPath = Path.Combine(directory, baseName + ".events.csv.inprogress");
            SavedPath = Path.Combine(directory, baseName + ".events.csv");
            writer = new StreamWriter(inProgressPath, false, new UTF8Encoding(false));
            writer.WriteLine("monotonic_seconds,event_type,balloon_id,x,y,z,speed,score,misses,practice_seed,sequence_mode");
            writer.Flush();
            EventCount = 0;
            monotonicClock.Restart();
            Log("round_start", 0, Vector3.zero, 0f, 0, 0, seed);
        }

        public void LogSpawn(int balloonId, Vector3 position, float speed, int score, int misses, int seed) =>
            Log("spawn", balloonId, position, speed, score, misses, seed);

        public void LogHit(int balloonId, Vector3 position, float speed, int score, int misses, int seed) =>
            Log("hit", balloonId, position, speed, score, misses, seed);

        public void LogMiss(int balloonId, Vector3 position, float speed, int score, int misses, int seed) =>
            Log("miss", balloonId, position, speed, score, misses, seed);

        public void CompleteTrial(int score, int misses, int seed)
        {
            Log("round_complete", 0, Vector3.zero, 0f, score, misses, seed);
            CloseTrial(true);
        }

        private void Log(string eventType, int balloonId, Vector3 position, float speed, int score, int misses, int seed)
        {
            if (writer == null) return;
            writer.WriteLine(string.Join(",",
                monotonicClock.Elapsed.TotalSeconds.ToString("F6", CultureInfo.InvariantCulture),
                eventType,
                balloonId.ToString(CultureInfo.InvariantCulture),
                position.x.ToString("F4", CultureInfo.InvariantCulture),
                position.y.ToString("F4", CultureInfo.InvariantCulture),
                position.z.ToString("F4", CultureInfo.InvariantCulture),
                speed.ToString("F4", CultureInfo.InvariantCulture),
                score.ToString(CultureInfo.InvariantCulture),
                misses.ToString(CultureInfo.InvariantCulture),
                seed.ToString(CultureInfo.InvariantCulture),
                activeSequenceMode));
            writer.Flush();
            EventCount++;
        }

        private void CloseTrial(bool cleanClose)
        {
            if (writer == null) return;
            writer.Dispose();
            writer = null;
            monotonicClock.Stop();
            if (cleanClose && File.Exists(inProgressPath)) File.Move(inProgressPath, SavedPath);
        }

        private void OnDestroy() => CloseTrial(false);
        private void OnApplicationQuit() => CloseTrial(false);
    }
}
