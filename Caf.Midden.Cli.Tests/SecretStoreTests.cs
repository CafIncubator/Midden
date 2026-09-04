using Caf.Midden.Cli.Security;
using System.Text.Json.Nodes;

namespace Caf.Midden.Cli.Tests;

public class SecretStoreTests : IDisposable
{
    private readonly string workingDirectory;

    public SecretStoreTests()
    {
        workingDirectory = Path.Combine(Path.GetTempPath(), $"midden-secretstore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
    }

    private string StorePath => SecretStore.GetDefaultPath(workingDirectory);

    private static string Password() => "correct horse battery staple";

    [Fact]
    public void Save_PasswordProtected_RoundTripsValues()
    {
        using (var store = SecretStore.Open(StorePath, Password, SecretProtectionProvider.Password))
        {
            store.Set("adls-prod", "s3cr3t");
            store.Save();
        }

        using var reopened = SecretStore.Open(StorePath, Password);

        Assert.True(reopened.TryGet("adls-prod", out var value));
        Assert.Equal("s3cr3t", value);
    }

    [Fact]
    public void Save_PasswordProtected_DoesNotWriteSecretInPlainText()
    {
        using (var store = SecretStore.Open(StorePath, Password, SecretProtectionProvider.Password))
        {
            store.Set("adls-prod", "s3cr3t");
            store.Save();
        }

        var contents = File.ReadAllText(StorePath);

        Assert.DoesNotContain("s3cr3t", contents, StringComparison.Ordinal);
        Assert.DoesNotContain("adls-prod", contents, StringComparison.Ordinal);
    }

    [Fact]
    public void Open_WrongPassword_ThrowsWithActionableMessage()
    {
        using (var store = SecretStore.Open(StorePath, Password, SecretProtectionProvider.Password))
        {
            store.Set("adls-prod", "s3cr3t");
            store.Save();
        }

        var exception = Assert.Throws<InvalidDataException>(
            () => SecretStore.Open(StorePath, () => "not the password"));

        Assert.Contains("password is incorrect", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Open_TamperedPayload_IsRejected()
    {
        using (var store = SecretStore.Open(StorePath, Password, SecretProtectionProvider.Password))
        {
            store.Set("adls-prod", "s3cr3t");
            store.Save();
        }

        // Flip a bit inside the authenticated payload; AES-GCM must refuse to decrypt it.
        var contents = File.ReadAllText(StorePath);
        var marker = "\"Payload\": \"";
        var start = contents.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var tampered = contents[start] == 'A' ? 'B' : 'A';
        File.WriteAllText(StorePath, contents[..start] + tampered + contents[(start + 1)..]);

        Assert.Throws<InvalidDataException>(() => SecretStore.Open(StorePath, Password));
    }

    [Fact]
    public void Open_InvalidBase64Payload_IsRejected()
    {
        using (var store = SecretStore.Open(StorePath, Password, SecretProtectionProvider.Password))
        {
            store.Set("adls-prod", "s3cr3t");
            store.Save();
        }

        var envelope = JsonNode.Parse(File.ReadAllText(StorePath))!.AsObject();
        envelope["Payload"] = "not valid Base64";
        File.WriteAllText(StorePath, envelope.ToJsonString());

        var exception = Assert.Throws<InvalidDataException>(() => SecretStore.Open(StorePath, Password));

        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Fact]
    public void Remove_ExistingSecret_RemovesItFromTheStore()
    {
        using (var store = SecretStore.Open(StorePath, Password, SecretProtectionProvider.Password))
        {
            store.Set("keep", "a");
            store.Set("drop", "b");
            store.Save();
        }

        using (var store = SecretStore.Open(StorePath, Password))
        {
            Assert.True(store.Remove("drop"));
            store.Save();
        }

        using var reopened = SecretStore.Open(StorePath, Password);

        Assert.False(reopened.TryGet("drop", out _));
        Assert.True(reopened.TryGet("keep", out _));
    }

    [Fact]
    public void Open_NewStore_IsNotWrittenUntilSaved()
    {
        using var store = SecretStore.Open(StorePath, Password, SecretProtectionProvider.Password);

        Assert.False(SecretStore.Exists(StorePath));
    }

    [Fact]
    public void Save_DpapiProtected_RoundTripsValues()
    {
        // DPAPI is the default on Windows and requires no password at all.
        if (!SecretStore.IsDpapiAvailable)
        {
            return;
        }

        using (var store = SecretStore.Open(StorePath, Password, SecretProtectionProvider.Dpapi))
        {
            store.Set("adls-prod", "s3cr3t");
            store.Save();
        }

        using var reopened = SecretStore.Open(StorePath, () => throw new InvalidOperationException("Should not prompt."));

        Assert.Equal(SecretProtectionProvider.Dpapi, reopened.Provider);
        Assert.True(reopened.TryGet("adls-prod", out var value));
        Assert.Equal("s3cr3t", value);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }
}
