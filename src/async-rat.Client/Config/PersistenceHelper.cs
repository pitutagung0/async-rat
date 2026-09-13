using System.Runtime.InteropServices;

namespace AsyncRat.Client.Config;

public enum PersistenceMethod
{
    None,
    StartupFolder,
    RegistryRunKey,
    ScheduledTask,
}

public sealed record PersistenceResult(PersistenceMethod Method, bool Success, string Message);

public static class PersistenceHelper
{
    public static PersistenceResult Install(ImplantConfig config, PersistenceMethod method)
    {
        if (!config.EnablePersistence)
            return new(PersistenceMethod.None, false, "Persistence disabled in config");

        return method switch
        {
            PersistenceMethod.StartupFolder => InstallStartupFolder(config),
            PersistenceMethod.RegistryRunKey => InstallRegistryKey(config),
            PersistenceMethod.ScheduledTask  => InstallScheduledTask(config),
            _ => new(method, false, "Unknown method"),
        };
    }

    private static PersistenceResult InstallStartupFolder(ImplantConfig cfg)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new(PersistenceMethod.StartupFolder, false, "Windows only");

        var startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var targetPath = Path.Combine(startupDir, $"{cfg.InstallName}.lnk");

        return new(PersistenceMethod.StartupFolder, true,
            $"[SIM] Would create shortcut at {targetPath}");
    }

    private static PersistenceResult InstallRegistryKey(ImplantConfig cfg)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new(PersistenceMethod.RegistryRunKey, false, "Windows only");

        const string keyPath = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        return new(PersistenceMethod.RegistryRunKey, true,
            $"[SIM] Would write {cfg.InstallName} to {keyPath}");
    }

    private static PersistenceResult InstallScheduledTask(ImplantConfig cfg)
    {
        var taskName = cfg.InstallName;
        return new(PersistenceMethod.ScheduledTask, true,
            $"[SIM] Would create scheduled task '{taskName}' with ONLOGON trigger");
    }

    public static PersistenceResult Uninstall(ImplantConfig config, PersistenceMethod method)
    {
        return new(method, true, $"[SIM] Would remove {method} persistence for {config.InstallName}");
    }

    public static List<PersistenceResult> DetectExisting(ImplantConfig config)
    {
        var results = new List<PersistenceResult>();

        var startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var lnkPath = Path.Combine(startupDir, $"{config.InstallName}.lnk");
        results.Add(new(PersistenceMethod.StartupFolder, File.Exists(lnkPath),
            File.Exists(lnkPath) ? $"Found: {lnkPath}" : "Not found"));

        results.Add(new(PersistenceMethod.RegistryRunKey, false, "[SIM] Check not implemented"));
        results.Add(new(PersistenceMethod.ScheduledTask, false, "[SIM] Check not implemented"));

        return results;
    }
}
