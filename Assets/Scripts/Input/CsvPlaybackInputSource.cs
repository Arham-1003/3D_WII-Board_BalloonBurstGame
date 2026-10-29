using System;
using System.Collections.Generic;
using System.Globalization;
using BalloonBalance.Core;
using BalloonBalance.SignalProcessing;
using UnityEngine;

namespace BalloonBalance.Input
{
    public sealed class CsvPlaybackInputSource : MonoBehaviour, ICopInputSource
    {
        [SerializeField] private TextAsset recording;
        [SerializeField, Min(0.05f)] private float playbackSpeed = 1f;
        [SerializeField] private bool loop;
        [SerializeField, Min(0f)] private float minimumTotalLoad = 10f;

        private readonly List<Row> rows = new List<Row>();
        private CopSample latest;
        private int index;
        private double playbackStart;
        private double sourceStart;
        private bool hasSample;

        public bool IsConnected => isActiveAndEnabled && rows.Count > 0;

        private void OnEnable()
        {
            LoadRows();
            Restart();
        }

        private void Update()
        {
            if (rows.Count == 0 || index >= rows.Count) return;
            double elapsed = (Time.realtimeSinceStartupAsDouble - playbackStart) * playbackSpeed;
            while (index < rows.Count && rows[index].time - sourceStart <= elapsed)
            {
                Publish(rows[index++]);
            }
            if (index >= rows.Count && loop) Restart();
        }

        public bool TryGetLatestSample(out CopSample sample) { sample = latest; return hasSample; }

        private void Restart()
        {
            index = 0;
            hasSample = false;
            playbackStart = Time.realtimeSinceStartupAsDouble;
            sourceStart = rows.Count > 0 ? rows[0].time : 0d;
        }

        private void Publish(Row row)
        {
            bool valid = CopCalculator.TryCalculate(row.fl, row.fr, row.rl, row.rr, minimumTotalLoad,
                out float total, out Vector2 cop, out CopValidityFlags flags);
            latest = new CopSample(row.sequence, row.time, Time.realtimeSinceStartupAsDouble,
                row.fl, row.fr, row.rl, row.rr, total, cop, cop, CopSourceType.CsvPlayback, flags,
                valid ? CopConnectionState.Connected : CopConnectionState.Faulted);
            hasSample = true;
        }

        private void LoadRows()
        {
            rows.Clear();
            if (recording == null) return;
            string[] lines = recording.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int line = 1; line < lines.Length; line++)
            {
                string[] cells = lines[line].Split(',');
                if (cells.Length < 6) continue;
                if (!long.TryParse(cells[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long sequence) ||
                    !double.TryParse(cells[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double time) ||
                    !TryFloat(cells[2], out float fl) || !TryFloat(cells[3], out float fr) ||
                    !TryFloat(cells[4], out float rl) || !TryFloat(cells[5], out float rr)) continue;
                rows.Add(new Row(sequence, time, fl, fr, rl, rr));
            }
        }

        private static bool TryFloat(string value, out float result) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

        private readonly struct Row
        {
            public readonly long sequence; public readonly double time;
            public readonly float fl, fr, rl, rr;
            public Row(long sequence, double time, float fl, float fr, float rl, float rr)
            { this.sequence = sequence; this.time = time; this.fl = fl; this.fr = fr; this.rl = rl; this.rr = rr; }
        }
    }
}
