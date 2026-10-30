using System.Reflection;
using BalloonBalance.Core;
using BalloonBalance.Input;
using NUnit.Framework;
using UnityEngine;

namespace BalloonBalance.Tests.EditMode
{
    public sealed class CopInputManagerTests
    {
        [Test]
        public void NonKeyboardSamplePublishesRawAndFilteredCopStreams()
        {
            GameObject host = new GameObject("CopInputManagerTestHost");
            try
            {
                MockInputSource source = host.AddComponent<MockInputSource>();
                source.SetSample(new CopSample(
                    sequenceNumber: 1,
                    sourceTimestampSeconds: 1d,
                    receiveTimestampSeconds: 1d,
                    frontLeft: 10f,
                    frontRight: 10f,
                    rearLeft: 10f,
                    rearRight: 10f,
                    totalLoad: 40f,
                    rawCop: new Vector2(0.5f, 0f),
                    filteredCop: Vector2.zero,
                    sourceType: CopSourceType.Udp,
                    validityFlags: CopValidityFlags.Valid,
                    connectionState: CopConnectionState.Connected));

                CopInputManager manager = host.AddComponent<CopInputManager>();
                SetInputSourceBehaviour(manager, source);
                manager.SendMessage("Awake");
                manager.SendMessage("Update");

                Assert.That(manager.HasValidSample, Is.True);
                Assert.That(manager.RawCopPosition, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(manager.FilteredCopPosition.x, Is.GreaterThan(0f));
                Assert.That(manager.FilteredCopPosition.x, Is.LessThan(manager.RawCopPosition.x));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void KeyboardSampleUsesFilteredStreamDirectly()
        {
            GameObject host = new GameObject("CopInputManagerKeyboardTestHost");
            try
            {
                MockInputSource source = host.AddComponent<MockInputSource>();
                source.SetSample(new CopSample(
                    sequenceNumber: 2,
                    sourceTimestampSeconds: 2d,
                    receiveTimestampSeconds: 2d,
                    frontLeft: 0f,
                    frontRight: 0f,
                    rearLeft: 0f,
                    rearRight: 0f,
                    totalLoad: 0f,
                    rawCop: new Vector2(0.2f, -0.1f),
                    filteredCop: new Vector2(0.4f, -0.2f),
                    sourceType: CopSourceType.Keyboard,
                    validityFlags: CopValidityFlags.Valid,
                    connectionState: CopConnectionState.Connected));

                CopInputManager manager = host.AddComponent<CopInputManager>();
                SetInputSourceBehaviour(manager, source);
                manager.SendMessage("Awake");
                manager.SendMessage("Update");

                Assert.That(manager.RawCopPosition, Is.EqualTo(new Vector2(0.2f, -0.1f)));
                Assert.That(manager.FilteredCopPosition, Is.EqualTo(new Vector2(0.4f, -0.2f)));
                Assert.That(manager.ControlPosition, Is.EqualTo(new Vector2(0.4f, -0.2f)));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static void SetInputSourceBehaviour(CopInputManager manager, MonoBehaviour source)
        {
            FieldInfo field = typeof(CopInputManager).GetField("inputSourceBehaviour", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(manager, source);
        }

        private sealed class MockInputSource : MonoBehaviour, ICopInputSource
        {
            private CopSample sample;

            public bool IsConnected { get; private set; } = true;

            public void SetSample(CopSample value)
            {
                sample = value;
            }

            public bool TryGetLatestSample(out CopSample latest)
            {
                latest = sample;
                return true;
            }
        }
    }
}
