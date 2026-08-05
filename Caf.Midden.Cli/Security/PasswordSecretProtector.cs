using System.Security.Cryptography;

namespace Caf.Midden.Cli.Security;

/// <summary>
/// Protects the secret store with AES-256-GCM using a key derived from a user supplied password.
/// Portable across operating systems, and used as the fallback when DPAPI is unavailable.
/// </summary>
public sealed class PasswordSecretProtector : ISecretProtector, IDisposable
{
    /// <summary>OWASP recommended minimum for PBKDF2-HMAC-SHA256.</summary>
    public const int DefaultIterations = 600_000;

    public const int SaltSizeBytes = 16;

    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] key;
    private bool disposed;

    public PasswordSecretProtector(string password, byte[] salt, int iterations)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentNullException.ThrowIfNull(salt);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        key = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeySizeBytes);
    }

    public static byte[] CreateSalt() => RandomNumberGenerator.GetBytes(SaltSizeBytes);

    public byte[] Protect(ReadOnlySpan<byte> plaintext)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var payload = new byte[NonceSizeBytes + TagSizeBytes + plaintext.Length];
        var nonce = payload.AsSpan(0, NonceSizeBytes);
        var tag = payload.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = payload.AsSpan(NonceSizeBytes + TagSizeBytes);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return payload;
    }

    public byte[] Unprotect(ReadOnlySpan<byte> payload)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (payload.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("The secret store payload is malformed or truncated.");
        }

        var nonce = payload[..NonceSizeBytes];
        var tag = payload.Slice(NonceSizeBytes, TagSizeBytes);
        var ciphertext = payload[(NonceSizeBytes + TagSizeBytes)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(key);
        disposed = true;
    }
}
