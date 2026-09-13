using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using AsyncRat.Client.Network;

namespace AsyncRat.Panel;

public sealed record SessionInfo(int Id, string RemoteEndpoint, DateTimeOffset ConnectedAt, TcpClient Tcp);

public sealed class SessionManager
{
    private readonly ConcurrentDictionary<int, SessionInfo> _sessions = new();
    private int _nextId;

    public int Register(TcpClient tcp)
    {
        var id = Interlocked.Increment(ref _nextId);
        var ep = tcp.Client.RemoteEndPoint?.ToString() ?? "unknown";
        _sessions[id] = new SessionInfo(id, ep, DateTimeOffset.UtcNow, tcp);
        return id;
    }

    public bool HasSession(int id) =>
        _sessions.ContainsKey(id) && _sessions[id].Tcp.Connected;

    public void PrintSessions()
    {
        Console.WriteLine($"{"ID",4}  {"Remote",22}  {"Connected",22}  {"Alive"}");
        Console.WriteLine(new string('-', 60));

        foreach (var s in _sessions.Values.OrderBy(x => x.Id))
        {
            var alive = s.Tcp.Connected ? "yes" : "no";
            Console.WriteLine($"{s.Id,4}  {s.RemoteEndpoint,22}  {s.ConnectedAt:yyyy-MM-dd HH:mm:ss,22}  {alive}");
        }

        if (_sessions.IsEmpty)
            Console.WriteLine("  (no sessions)");
    }

    public async Task<string> SendCommandAsync(int id, string command)
    {
        if (!_sessions.TryGetValue(id, out var session) || !session.Tcp.Connected)
            return "[!] Session not available";

        try
        {
            var framer = new PacketFramer();
            var stream = session.Tcp.GetStream();

            var payload = Encoding.UTF8.GetBytes(command);
            var frame = framer.Frame(payload);
            await stream.WriteAsync(frame);
            await stream.FlushAsync();

            var response = await framer.ReadFrameAsync(stream);
            return Encoding.UTF8.GetString(response);
        }
        catch (Exception ex)
        {
            return $"[!] Error: {ex.Message}";
        }
    }

    public void Remove(int id)
    {
        if (_sessions.TryRemove(id, out var session))
        {
            session.Tcp.Close();
            session.Tcp.Dispose();
        }
    }

    public void DisconnectAll()
    {
        foreach (var id in _sessions.Keys.ToList())
            Remove(id);
    }
}
