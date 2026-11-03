using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BalloonBalance.Core;
using BalloonBalance.SignalProcessing;
using UnityEngine;

namespace BalloonBalance.Input
{
    public sealed class UdpCopInputSource : MonoBehaviour, IBufferedCopInputSource
    {
        [SerializeField, Range(1024, 65535)] private int port = 5005;
        [SerializeField, Min(0.05f)] private float staleAfterSeconds = 0.25f;
        [SerializeField, Min(0f)] private float minimumTotalLoad = 10f;

        private readonly ConcurrentQueue<string> packets = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<CopSample> samples = new ConcurrentQueue<CopSample>();
        private UdpClient client;
        private Thread receiveThread;
        private volatile bool running;
        private CopSample latest;
        private bool hasSample;
        private long lastSequence = -1;
        private double lastReceiveTime;

        public bool IsConnected => hasSample && Time.realtimeSinceStartupAsDouble - lastReceiveTime <= staleAfterSeconds;
        public int MalformedPacketCount { get; private set; }
        public int DuplicatePacketCount { get; private set; }
        public int OutOfOrderPacketCount { get; private set; }
        public int MissingSequenceCount { get; private set; }

        private void OnEnable()
        {
            lastSequence = -1;
            lastReceiveTime = 0d;
            hasSample = false;
            try
            {
                client = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
                running = true;
                receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "BalloonBalance UDP" };
                receiveThread.Start();
            }
            catch (Exception exception)
            {
                Debug.LogError($"UDP input could not bind to 127.0.0.1:{port}: {exception.Message}", this);
                enabled = false;
            }
        }

        private void Update()
        {
            while (packets.TryDequeue(out string json)) ProcessPacket(json);
        }

        public bool TryGetLatestSample(out CopSample sample)
        {
            sample = latest;
            return IsConnected && latest.IsValid;
        }

        public bool TryDequeueSample(out CopSample sample) => samples.TryDequeue(out sample);

        private void ProcessPacket(string json)
        {
            BoardPacket packet;
            try { packet = JsonUtility.FromJson<BoardPacket>(json); }
            catch { MalformedPacketCount++; return; }
            if (packet == null || packet.version != 1 || packet.sequence < 0 || !packet.connected)
            { MalformedPacketCount++; return; }
            if (packet.sequence == lastSequence) { DuplicatePacketCount++; return; }
            if (packet.sequence < lastSequence) { OutOfOrderPacketCount++; return; }
            if (lastSequence >= 0 && packet.sequence > lastSequence + 1)
                MissingSequenceCount += (int)Math.Min(int.MaxValue, packet.sequence - lastSequence - 1);

            double receiveTime = Time.realtimeSinceStartupAsDouble;
            bool valid = CopCalculator.TryCalculate(packet.frontLeft, packet.frontRight, packet.rearLeft, packet.rearRight,
                minimumTotalLoad, out float total, out Vector2 cop, out CopValidityFlags flags);
            latest = new CopSample(packet.sequence, packet.sourceTime, receiveTime, packet.frontLeft, packet.frontRight,
                packet.rearLeft, packet.rearRight, total, cop, cop, CopSourceType.Udp, flags,
                valid ? CopConnectionState.Connected : CopConnectionState.Faulted);
            lastSequence = packet.sequence;
            lastReceiveTime = receiveTime;
            hasSample = true;
            samples.Enqueue(latest);
        }

        private void ReceiveLoop()
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            while (running)
            {
                try
                {
                    byte[] data = client.Receive(ref remote);
                    packets.Enqueue(Encoding.UTF8.GetString(data));
                }
                catch (SocketException) { if (!running) return; }
                catch (ObjectDisposedException) { return; }
            }
        }

        private void OnDisable()
        {
            running = false;
            client?.Close();
            if (receiveThread != null && receiveThread.IsAlive) receiveThread.Join(100);
            client = null;
            receiveThread = null;
            while (packets.TryDequeue(out _)) { }
            while (samples.TryDequeue(out _)) { }
            hasSample = false;
            lastSequence = -1;
            lastReceiveTime = 0d;
        }

        [Serializable]
        private sealed class BoardPacket
        {
            public int version;
            public long sequence;
            public double sourceTime;
            public float frontLeft, frontRight, rearLeft, rearRight, totalLoad;
            public bool connected;
        }
    }
}
