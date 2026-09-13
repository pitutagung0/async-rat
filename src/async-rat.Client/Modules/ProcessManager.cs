using System.Diagnostics;
using System.Text;

namespace AsyncRat.Client.Modules;

public sealed class ProcessManager
{
    public string ListProcesses()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{"PID",8}  {"Memory (MB)",12}  {"Name"}");
        sb.AppendLine(new string('-', 50));

        Process[] procs;
        try
        {
            procs = Process.GetProcesses();
        }
        catch (Exception ex)
        {
            return $"ERR|{ex.Message}";
        }

        foreach (var p in procs.OrderByDescending(SafeWorkingSet))
        {
            try
            {
                var mem = p.WorkingSet64 / (1024.0 * 1024.0);
                sb.AppendLine($"{p.Id,8}  {mem,12:F1}  {p.ProcessName}");
            }
            catch (Exception)
            {
                sb.AppendLine($"{p.Id,8}  {"N/A",12}  {p.ProcessName}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    public string KillProcess(int pid)
    {
        try
        {
            var proc = Process.GetProcessById(pid);
            var name = proc.ProcessName;
            proc.Kill(entireProcessTree: true);
            return $"OK|Killed {name} (PID {pid})";
        }
        catch (ArgumentException)
        {
            return $"ERR|No process with PID {pid}";
        }
        catch (Exception ex)
        {
            return $"ERR|{ex.Message}";
        }
    }

    public string GetProcessInfo(int pid)
    {
        try
        {
            var p = Process.GetProcessById(pid);
            var sb = new StringBuilder();
            sb.AppendLine($"Name       : {p.ProcessName}");
            sb.AppendLine($"PID        : {p.Id}");
            sb.AppendLine($"Memory     : {p.WorkingSet64 / (1024.0 * 1024.0):F1} MB");
            sb.AppendLine($"Threads    : {p.Threads.Count}");
            sb.AppendLine($"Start Time : {p.StartTime:yyyy-MM-dd HH:mm:ss}");
            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"ERR|{ex.Message}";
        }
    }

    private static long SafeWorkingSet(Process p)
    {
        try { return p.WorkingSet64; }
        catch { return 0; }
    }
}
