using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Caf.Midden.Cli.Security;

/// <summary>
/// An encrypted name/value store for data store credentials, kept alongside 'configuration.json'.
/// The configuration file itself stays plain text and human readable; only the credential values
/// live here, referenced from the configuration by name.
/// </summary>
public sealed class SecretStore : IDisposable
{
    public const string StoreFileName = "secrets.midden";

    private static readonly JsonSerializerOptions EnvelopeJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly string storePath;
    private readonly SecretStoreFile envelope;
    private readonly ISecretProtector protector;
    private readonly Dictionary<string, string> entries;

    private SecretStore(
        string storePath,
        SecretStoreFile envelope,
        ISecretProtector protector,
        Dictionary<string, string> entries)
    {
        this.storePath = storePath;
        this.envelope = envelope;
        this.protector = protector;
        this.entries = entries;
    }

    public SecretProtectionProvider Provider => envelope.Provider;

    public IReadOnlyCollection<string> Names => entries.Keys;

    /// <summary>
    /// True when DPAPI can be used, meaning the store can be opened without prompting for a password.
    /// </summary>
    public static bool IsDpapiAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static SecretProtectionProvider DefaultProvider =>
        IsDpapiAvailable ? SecretProtectionProvider.Dpapi : SecretProtectionProvider.Password;

    public static string GetDefaultPath(string configurationDirectory) =>
        Path.Combine(configurationDirectory, StoreFileName);

    public static bool Exists(string storePath) => File.Exists(storePath);

    /// <summary>
    /// Opens an existing store, or creates an empty in-memory one when the file does not exist.
    /// </summary>
    /// <param name="passwordProvider">
    /// Invoked only when the store is password protected. Kept as a callback so that callers which
    /// never touch a password protected store are never prompted.
    /// </param>
    public static SecretStore Open(
        string storePath,
        Func<string> passwordProvider,
        SecretProtectionProvider? providerForNewStore = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storePath);
        ArgumentNullException.ThrowIfNull(passwordProvider);

        return File.Exists(storePath)
            ? OpenExisting(storePath, passwordProvider)
            : CreateNew(storePath, passwordProvider, providerForNewStore ?? DefaultProvider);
    }

    public bool TryGet(string name, out string? value) => entries.TryGetValue(name, out value);

    public void Set(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        entries[name] = value;
    }

    public bool Remove(string name) => entries.Remove(name);

    public void Save()
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(entries);

        try
        {
            envelope.Payload = Convert.ToBase64String(protector.Protect(plaintext));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }

        var directory = Path.GetDirectoryName(storePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to a temporary file first so an interrupted save cannot destroy existing secrets.
        var temporaryPath = storePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(envelope, EnvelopeJsonOptions));
        RestrictToCurrentUser(temporaryPath);
        File.Move(temporaryPath, storePath, overwrite: true);
    }

    public void Dispose() => (protector as IDisposable)?.Dispose();

    private static SecretStore OpenExisting(string storePath, Func<string> passwordProvider)
    {
        var envelope = JsonSerializer.Deserialize<SecretStoreFile>(File.ReadAllText(storePath), EnvelopeJsonOptions)
            ?? throw new InvalidDataException($"Secret store '{storePath}' is empty or invalid.");

        if (envelope.Version != SecretStoreFile.CurrentVersion)
        {
            throw new InvalidDataException(
                $"Secret store '{storePath}' uses version '{envelope.Version}', but this version of the CLI only understands '{SecretStoreFile.CurrentVersion}'.");
        }

        var protector = CreateProtector(envelope, passwordProvider, storePath);

        Dictionary<string, string> entries;

        try
        {
            var plaintext = protector.Unprotect(Convert.FromBase64String(envelope.Payload));

            try
            {
                entries = JsonSerializer.Deserialize<Dictionary<string, string>>(plaintext) ?? [];
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            (protector as IDisposable)?.Dispose();

            throw new InvalidDataException(
                envelope.Provider == SecretProtectionProvider.Password
                    ? $"Unable to decrypt '{storePath}'. The password is incorrect, or the file has been modified."
                    : $"Unable to decrypt '{storePath}'. It was created by a different Windows user or on a different machine.",
                exception);
        }

        return new SecretStore(storePath, envelope, protector, entries);
    }

    private static SecretStore CreateNew(
        string storePath,
        Func<string> passwordProvider,
        SecretProtectionProvider provider)
    {
        if (provider == SecretProtectionProvider.Dpapi && !IsDpapiAvailable)
        {
            throw new PlatformNotSupportedException(
                "DPAPI protection is only available on Windows. Re-run with '--password' to create a portable, password protected store.");
        }

        var envelope = new SecretStoreFile { Provider = provider };

        if (provider == SecretProtectionProvider.Password)
        {
            envelope.Salt = Convert.ToBase64String(PasswordSecretProtector.CreateSalt());
            envelope.Iterations = PasswordSecretProtector.DefaultIterations;
        }

        return new SecretStore(storePath, envelope, CreateProtector(envelope, passwordProvider, storePath), []);
    }

    private static ISecretProtector CreateProtector(
        SecretStoreFile envelope,
        Func<string> passwordProvider,
        string storePath)
    {
        switch (envelope.Provider)
        {
            case SecretProtectionProvider.Dpapi:
                if (!IsDpapiAvailable)
                {
                    throw new PlatformNotSupportedException(
                        $"Secret store '{storePath}' is protected with the Windows Data Protection API and cannot be opened on this platform.");
                }

#pragma warning disable CA1416 // Guarded above by IsDpapiAvailable, which checks OperatingSystem.IsWindows().
                return new DpapiSecretProtector();
#pragma warning restore CA1416

            case SecretProtectionProvider.Password:
                if (string.IsNullOrWhiteSpace(envelope.Salt))
                {
                    throw new InvalidDataException($"Secret store '{storePath}' is password protected but has no salt.");
                }

                return new PasswordSecretProtector(
                    passwordProvider(),
                    Convert.FromBase64String(envelope.Salt),
                    envelope.Iterations > 0 ? envelope.Iterations : PasswordSecretProtector.DefaultIterations);

            default:
                throw new InvalidDataException($"Secret store '{storePath}' uses an unknown protection provider '{envelope.Provider}'.");
        }
    }

    private static void RestrictToCurrentUser(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
