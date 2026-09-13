using System.Text;

namespace AsyncRat.Client.Modules;

public sealed class ReverseShell
{
    private readonly Dictionary<string, Func<string, string>> _builtins;

    public ReverseShell()
    {
        _builtins = new(StringComparer.OrdinalIgnoreCase)
        {
            ["whoami"]   = _ => $"{Environment.UserDomainName}\\{Environment.UserName}",
            ["hostname"] = _ => Environment.MachineName,
            ["pwd"]      = _ => Environment.CurrentDirectory,
            ["cd"]       = ChangeDirectory,
            ["dir"]      = ListDir,
            ["ls"]       = ListDir,
            ["echo"]     = arg => arg,
            ["env"]      = _ => FormatEnvVars(),
            ["id"]       = _ => $"uid=1000({Environment.UserName}) gid=1000",
            ["uname"]    = _ => $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}",
        };
    }

    public string Execute(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return string.Empty;

        var parts = command.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0];
        var arg = parts.Length > 1 ? parts[1] : string.Empty;

        if (_builtins.TryGetValue(cmd, out var handler))
            return handler(arg);

        return $"[simulated] {command}\n"
             + $"Process exited with code 0\n"
             + $"(shell simulation — no real execution)";
    }

    private static string ChangeDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return Environment.CurrentDirectory;
        if (Directory.Exists(path))
        {
            Environment.CurrentDirectory = Path.GetFullPath(path);
            return Environment.CurrentDirectory;
        }
        return $"cd: no such directory: {path}";
    }

    private static string ListDir(string path)
    {
        var target = string.IsNullOrWhiteSpace(path) ? "." : path;
        if (!Directory.Exists(target))
            return $"ls: cannot access '{target}': No such file or directory";

        var sb = new StringBuilder();
        foreach (var d in Directory.GetDirectories(target))
            sb.AppendLine($"d  {Path.GetFileName(d)}/");
        foreach (var f in Directory.GetFiles(target))
        {
            var info = new FileInfo(f);
            sb.AppendLine($"-  {info.Length,10}  {info.LastWriteTime:yyyy-MM-dd HH:mm}  {info.Name}");
        }
        return sb.Length > 0 ? sb.ToString().TrimEnd() : "(empty)";
    }

    private static string FormatEnvVars()
    {
        var sb = new StringBuilder();
        foreach (var key in new[] { "PATH", "HOME", "USERPROFILE", "COMPUTERNAME", "OS", "TEMP" })
        {
            var val = Environment.GetEnvironmentVariable(key);
            if (val is not null) sb.AppendLine($"{key}={val}");
        }
        return sb.ToString().TrimEnd();
    }
}
