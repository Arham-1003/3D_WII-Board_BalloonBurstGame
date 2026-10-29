using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

const int sampleRateHz = 50;
const float totalLoad = 70f;
using UdpClient udp = new();
IPEndPoint destination = new(IPAddress.Loopback, 5005);
Directory.CreateDirectory("Logs");
string path = Path.Combine("Logs", $"synthetic_raw_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv.inprogress");
using StreamWriter log = new(path, false, new UTF8Encoding(false)) { AutoFlush = true };
log.WriteLine("sequence,source_time,front_left,front_right,rear_left,rear_right,total_load");
Stopwatch clock = Stopwatch.StartNew();
long sequence = 0;
Console.WriteLine($"Streaming version 1 packets to {destination}. Ctrl+C to stop. Raw log: {path}");
while (true)
{
    double t = clock.Elapsed.TotalSeconds;
    float x = MathF.Sin((float)t * 0.7f) * 0.55f;
    float y = MathF.Sin((float)t * 0.43f) * 0.4f;
    float fl = totalLoad * 0.25f * (1f - x) * (1f + y);
    float fr = totalLoad * 0.25f * (1f + x) * (1f + y);
    float rl = totalLoad * 0.25f * (1f - x) * (1f - y);
    float rr = totalLoad * 0.25f * (1f + x) * (1f - y);
    sequence++;
    string csv = string.Join(',', sequence, F(t), F(fl), F(fr), F(rl), F(rr), F(totalLoad));
    log.WriteLine(csv);
    string json = $"{{\"version\":1,\"sequence\":{sequence},\"sourceTime\":{F(t)},\"frontLeft\":{F(fl)},\"frontRight\":{F(fr)},\"rearLeft\":{F(rl)},\"rearRight\":{F(rr)},\"totalLoad\":{F(totalLoad)},\"connected\":true}}";
    byte[] bytes = Encoding.UTF8.GetBytes(json);
    await udp.SendAsync(bytes, destination);
    await Task.Delay(1000 / sampleRateHz);
}

static string F(double value) => value.ToString("F6", CultureInfo.InvariantCulture);
