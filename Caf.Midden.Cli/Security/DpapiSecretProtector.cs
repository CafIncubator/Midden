using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Caf.Midden.Cli.Security;

/// <summary>
/// Protects the secret store using the Windows Data Protection API scoped to the current user.
/// This requires no password, which keeps the common researcher workflow prompt-free, while making
/// the file inert if it is copied to another machine or opened by another user account.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiSecretProtector : ISecretProtector
{
    // Additional entropy bound to this application, so an unrelated process cannot trivially
    // unprotect the payload just by running as the same user.
    private static readonly byte[] Entropy = "Caf.Midden.Cli.SecretStore.v1"u8.ToArray();

    public byte[] Protect(ReadOnlySpan<byte> plaintext) =>
        ProtectedData.Protect(plaintext.ToArray(), Entropy, DataProtectionScope.CurrentUser);

    public byte[] Unprotect(ReadOnlySpan<byte> payload) =>
        ProtectedData.Unprotect(payload.ToArray(), Entropy, DataProtectionScope.CurrentUser);
}
