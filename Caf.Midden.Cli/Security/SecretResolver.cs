using System.Text;

namespace Caf.Midden.Cli.Security;

/// <summary>
/// Resolves credential values referenced from 'configuration.json'.
/// <para>
/// A configuration value of the form 'secret:name' is looked up, in order, from:
/// <list type="number">
/// <item>the environment variable 'MIDDEN_SECRET_NAME', so containers and Azure App Service can
/// supply credentials with no password prompt and no encrypted file deployed;</item>
/// <item>the encrypted local secret store, for interactive desktop use.</item>
/// </list>
/// Any other value is treated as a literal credential for backwards compatibility.
/// </para>
/// </summary>
public sealed class SecretResolver : IDisposable
{
    public const string SecretReferencePrefix = "secret:";
    public const string EnvironmentVariablePrefix = "MIDDEN_SECRET_";

    private readonly Lazy<SecretStore?> store;

    public SecretResolver(string storePath, Func<string> passwordProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storePath);
        ArgumentNullException.ThrowIfNull(passwordProvider);

        // Opened lazily so that a configuration containing no secret references never prompts for a
        // password and never requires the store to exist at all.
        store = new Lazy<SecretStore?>(() => SecretStore.Exists(storePath)
            ? SecretStore.Open(storePath, passwordProvider)
            : null);
    }

    /// <summary>
    /// Converts a secret name into its environment variable equivalent, for example
    /// 'adls-prod' becomes 'MIDDEN_SECRET_ADLS_PROD'.
    /// </summary>
    public static string GetEnvironmentVariableName(string secretName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        var builder = new StringBuilder(EnvironmentVariablePrefix.Length + secretName.Length);
        builder.Append(EnvironmentVariablePrefix);

        foreach (var character in secretName)
        {
            builder.Append(char.IsAsciiLetterOrDigit(character) ? char.ToUpperInvariant(character) : '_');
        }

        return builder.ToString();
    }

    public static bool IsSecretReference(string? value) =>
        value is not null && value.StartsWith(SecretReferencePrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves a configuration value that may be a literal credential or a 'secret:name' reference.
    /// </summary>
    /// <exception cref="InvalidOperationException">The reference could not be resolved from any source.</exception>
    public SecretResolution Resolve(string? configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return new SecretResolution(null, SecretSource.NotProvided, null);
        }

        if (!IsSecretReference(configuredValue))
        {
            return new SecretResolution(configuredValue, SecretSource.Literal, null);
        }

        var secretName = configuredValue[SecretReferencePrefix.Length..].Trim();

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new InvalidOperationException(
                $"'{configuredValue}' is not a valid secret reference. Expected the form '{SecretReferencePrefix}name'.");
        }

        var environmentVariableName = GetEnvironmentVariableName(secretName);
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);

        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return new SecretResolution(environmentValue, SecretSource.EnvironmentVariable, secretName);
        }

        if (store.Value is { } secretStore && secretStore.TryGet(secretName, out var storedValue) && storedValue is not null)
        {
            return new SecretResolution(storedValue, SecretSource.SecretStore, secretName);
        }

        throw new InvalidOperationException(
            $"The secret '{secretName}' could not be found. Set it with 'midden secret set {secretName}', "
            + $"or supply it through the '{environmentVariableName}' environment variable.");
    }

    public void Dispose()
    {
        if (store.IsValueCreated)
        {
            store.Value?.Dispose();
        }
    }
}

/// <summary>The outcome of resolving a single credential value.</summary>
/// <param name="Value">The resolved credential, or null when nothing was configured.</param>
/// <param name="Source">Where the credential came from.</param>
/// <param name="Name">The secret name, when the configured value was a reference.</param>
public readonly record struct SecretResolution(string? Value, SecretSource Source, string? Name);
