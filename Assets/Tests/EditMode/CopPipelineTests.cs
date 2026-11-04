using BalloonBalance.Calibration;
using BalloonBalance.Core;
using BalloonBalance.SignalProcessing;
using NUnit.Framework;
using UnityEngine;

namespace BalloonBalance.Tests.EditMode
{
    public sealed class CopPipelineTests
    {
        [Test]
        public void CalculatorProducesExpectedSignsAndRejectsLowLoad()
        {
            Assert.That(CopCalculator.TryCalculate(10f, 30f, 10f, 30f, 5f,
                out float total, out Vector2 right, out CopValidityFlags flags), Is.True);
            Assert.That(total, Is.EqualTo(80f));
            Assert.That(right.x, Is.Positive);
            Assert.That(right.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(flags, Is.EqualTo(CopValidityFlags.Valid));

            Assert.That(CopCalculator.TryCalculate(0.1f, 0.1f, 0.1f, 0.1f, 5f,
                out _, out _, out flags), Is.False);
            Assert.That(flags, Is.EqualTo(CopValidityFlags.LowTotalLoad));
        }

        [Test]
        public void AsymmetricMappingRemapsDeadZoneAndClamps()
        {
            CalibrationProfile profile = new CalibrationProfile
            {
                neutral = new Vector2(0.1f, -0.05f), leftRange = 0.2f, rightRange = 0.4f,
                forwardRange = 0.3f, backwardRange = 0.25f, deadZoneX = 0.1f, deadZoneY = 0.1f
            };
            Assert.That(CopMapper.Map(profile.neutral, profile), Is.EqualTo(Vector2.zero));
            Assert.That(CopMapper.Map(new Vector2(0.5f, 0.25f), profile), Is.EqualTo(Vector2.one));
            Assert.That(CopMapper.Map(new Vector2(-1f, -1f), profile), Is.EqualTo(-Vector2.one));
        }

        [Test]
        public void DefaultCalibrationReachesBoundsWithComfortableLean()
        {
            CalibrationProfile profile = new CalibrationProfile { deadZoneX = 0f, deadZoneY = 0f };
            Assert.That(CopMapper.Map(new Vector2(-0.1f, 0f), profile).x, Is.EqualTo(-1f));
            Assert.That(CopMapper.Map(new Vector2(0.1f, 0f), profile).x, Is.EqualTo(1f));
            Assert.That(CopMapper.Map(new Vector2(0f, -0.1f), profile).y, Is.EqualTo(-1f));
            Assert.That(CopMapper.Map(new Vector2(0f, 0.1f), profile).y, Is.EqualTo(1f));
        }

        [Test]
        public void CausalFiltersConvergeToConstantInput()
        {
            ICopFilter[] filters = { new EmaCopFilter(2f), new MovingAverageCopFilter(5),
                new ButterworthCopFilter(2f, 50f), new MedianCopFilter(3),
                new AdaptiveLowPassCopFilter(1.5f, 3f), new AlphaBetaCopFilter(0.85f, 0.05f) };
            foreach (ICopFilter filter in filters)
            {
                filter.Reset();
                Vector2 output = Vector2.zero;
                for (int index = 0; index < 200; index++) output = filter.Process(Vector2.one, 0.02f);
                Assert.That(output.x, Is.EqualTo(1f).Within(0.02f), filter.Name);
                Assert.That(output.y, Is.EqualTo(1f).Within(0.02f), filter.Name);
            }
        }

        [Test]
        public void AdaptiveFilterRespondsFasterWhileInputIsMoving()
        {
            AdaptiveLowPassCopFilter filter = new AdaptiveLowPassCopFilter(1f, 4f);
            filter.Process(Vector2.zero, 0.05f);
            Vector2 first = filter.Process(Vector2.one, 0.05f);
            Assert.That(first.x, Is.GreaterThan(0.3f));
            Assert.That(first.x, Is.LessThan(1f));
        }

        [Test]
        public void AlphaBetaFilterConvergesAfterAStep()
        {
            AlphaBetaCopFilter filter = new AlphaBetaCopFilter(0.85f, 0.05f);
            filter.Process(Vector2.zero, 0.05f);
            Vector2 output = Vector2.zero;
            for (int index = 0; index < 100; index++) output = filter.Process(Vector2.one, 0.05f);
            Assert.That(output.x, Is.EqualTo(1f).Within(0.02f));
            Assert.That(output.y, Is.EqualTo(1f).Within(0.02f));
        }


        [Test]
        public void MedianFilterRejectsAnIsolatedSpike()
        {
            MedianCopFilter filter = new MedianCopFilter(3);
            filter.Process(Vector2.zero, 0.02f);
            filter.Process(Vector2.zero, 0.02f);
            Vector2 output = filter.Process(new Vector2(10f, -10f), 0.02f);
            Assert.That(output, Is.EqualTo(Vector2.zero));
        }
    }
}
