using System.Text.Json.Serialization;

namespace Caf.Midden.Cli.Security;

/// <summary>
/// On-disk envelope for the encrypted secret store. Only the <see cref="Payload"/> is encrypted;
/// the surrounding metadata is required to decrypt it and is not itself sensitive.
/// </summary>
internal sealed class SecretStoreFile
{
    public const string CurrentVersion = "1.0.0";

    public string Version { get; set; } = CurrentVersion;

    public SecretProtectionProvider Provider { get; set; }

    /// <summary>PBKDF2 salt, base64 encoded. Only present for <see cref="SecretProtectionProvider.Password"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Salt { get; set; }

    /// <summary>PBKDF2 iteration count. Persisted so the cost can be raised in future versions.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Iterations { get; set; }

    /// <summary>Encrypted secret name/value map, base64 encoded.</summary>
    public string Payload { get; set; } = string.Empty;
}
