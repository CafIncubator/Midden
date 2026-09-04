namespace Caf.Midden.Cli.Security;

/// <summary>
/// Encrypts and decrypts the secret store payload. Implementations are responsible for embedding
/// whatever per-operation material they need (nonces, tags) into the returned payload.
/// </summary>
public interface ISecretProtector
{
    byte[] Protect(ReadOnlySpan<byte> plaintext);

    byte[] Unprotect(ReadOnlySpan<byte> payload);
}
