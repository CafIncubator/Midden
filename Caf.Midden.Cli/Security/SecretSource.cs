namespace Caf.Midden.Cli.Security;

/// <summary>
/// Where a resolved secret value came from. Used by callers to warn about discouraged sources.
/// </summary>
public enum SecretSource
{
    NotProvided,

    /// <summary>The value was written directly into 'configuration.json'. Supported, but discouraged.</summary>
    Literal,

    /// <summary>The value came from a MIDDEN_SECRET_* environment variable. Preferred for unattended runs.</summary>
    EnvironmentVariable,

    /// <summary>The value came from the encrypted local secret store. Preferred for interactive use.</summary>
    SecretStore,
}
