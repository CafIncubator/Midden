namespace Caf.Midden.Cli.Security;

/// <summary>
/// Identifies how the local secret store payload is encrypted at rest.
/// </summary>
public enum SecretProtectionProvider
{
    /// <summary>
    /// Windows Data Protection API, scoped to the current user. Requires no password, and the
    /// resulting file cannot be read by another user or on another machine.
    /// </summary>
    Dpapi,

    /// <summary>
    /// PBKDF2-derived key with AES-256-GCM. Portable across operating systems, but requires the
    /// user to supply a password.
    /// </summary>
    Password,
}
