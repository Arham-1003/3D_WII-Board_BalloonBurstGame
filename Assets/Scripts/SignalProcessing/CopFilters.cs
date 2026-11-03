using UnityEngine;

namespace BalloonBalance.SignalProcessing
{
    public sealed class PassThroughCopFilter : ICopFilter
    {
        public string Name => "Raw";
        public void Reset() { }
        public Vector2 Process(Vector2 rawCop, float deltaTime) => rawCop;
    }

    public sealed class EmaCopFilter : ICopFilter
    {
        private readonly float cutoffHz;
        private Vector2 state;
        private bool initialized;
        public string Name => "EMA";

        public EmaCopFilter(float cutoffHz) => this.cutoffHz = Mathf.Max(0.01f, cutoffHz);
        public void Reset() { initialized = false; state = Vector2.zero; }
        public Vector2 Process(Vector2 rawCop, float deltaTime)
        {
            if (!initialized) { initialized = true; return state = rawCop; }
            float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * cutoffHz * Mathf.Max(0f, deltaTime));
            return state += alpha * (rawCop - state);
        }
    }

    public sealed class MovingAverageCopFilter : ICopFilter
    {
        private readonly Vector2[] buffer;
        private int index;
        private int count;
        private Vector2 sum;
        public string Name => "MovingAverage";

        public MovingAverageCopFilter(int windowSize) => buffer = new Vector2[Mathf.Max(1, windowSize)];
        public void Reset() { index = count = 0; sum = Vector2.zero; }
        public Vector2 Process(Vector2 rawCop, float deltaTime)
        {
            if (count == buffer.Length) sum -= buffer[index]; else count++;
            buffer[index] = rawCop;
            sum += rawCop;
            index = (index + 1) % buffer.Length;
            return sum / count;
        }
    }

    public sealed class MedianCopFilter : ICopFilter
    {
        private readonly Vector2[] buffer;
        private readonly float[] sortedX;
        private readonly float[] sortedY;
        private int index;
        private int count;
        public string Name => "Median";

        public MedianCopFilter(int windowSize)
        {
            int size = Mathf.Max(1, windowSize);
            if (size % 2 == 0) size++;
            buffer = new Vector2[size];
            sortedX = new float[size];
            sortedY = new float[size];
        }

        public void Reset() { index = count = 0; }

        public Vector2 Process(Vector2 rawCop, float deltaTime)
        {
            buffer[index] = rawCop;
            index = (index + 1) % buffer.Length;
            count = Mathf.Min(count + 1, buffer.Length);

            for (int sample = 0; sample < count; sample++)
            {
                sortedX[sample] = buffer[sample].x;
                sortedY[sample] = buffer[sample].y;
            }
            System.Array.Sort(sortedX, 0, count);
            System.Array.Sort(sortedY, 0, count);
            int middle = count / 2;
            if (count % 2 == 1) return new Vector2(sortedX[middle], sortedY[middle]);
            return new Vector2((sortedX[middle - 1] + sortedX[middle]) * 0.5f,
                (sortedY[middle - 1] + sortedY[middle]) * 0.5f);
        }
    }

    public sealed class ButterworthCopFilter : ICopFilter
    {
        private readonly float b0, b1, b2, a1, a2;
        private Vector2 x1, x2, y1, y2;
        public string Name => "Butterworth2";

        public ButterworthCopFilter(float cutoffHz, float sampleRateHz)
        {
            float safeRate = Mathf.Max(2f, sampleRateHz);
            float safeCutoff = Mathf.Clamp(cutoffHz, 0.01f, safeRate * 0.49f);
            float k = Mathf.Tan(Mathf.PI * safeCutoff / safeRate);
            float norm = 1f / (1f + Mathf.Sqrt(2f) * k + k * k);
            b0 = k * k * norm; b1 = 2f * b0; b2 = b0;
            a1 = 2f * (k * k - 1f) * norm;
            a2 = (1f - Mathf.Sqrt(2f) * k + k * k) * norm;
        }

        public void Reset() { x1 = x2 = y1 = y2 = Vector2.zero; }
        public Vector2 Process(Vector2 rawCop, float deltaTime)
        {
            Vector2 output = b0 * rawCop + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = rawCop; y2 = y1; y1 = output;
            return output;
        }
    }
}
