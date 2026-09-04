using Caf.Midden.Cli.Security;

namespace Caf.Midden.Cli.Tests;

public class SecretResolverTests : IDisposable
{
    private readonly string workingDirectory;
    private readonly List<string> environmentVariablesToClear = [];

    public SecretResolverTests()
    {
        workingDirectory = Path.Combine(Path.GetTempPath(), $"midden-resolver-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
    }

    private string StorePath => SecretStore.GetDefaultPath(workingDirectory);

    private static string Password() => "correct horse battery staple";

    private SecretResolver CreateResolver() => new(StorePath, Password);

    private void SetEnvironmentVariable(string name, string? value)
    {
        Environment.SetEnvironmentVariable(name, value);
        environmentVariablesToClear.Add(name);
    }

    private void GivenStoredSecret(string name, string value)
    {
        using var store = SecretStore.Open(StorePath, Password, SecretProtectionProvider.Password);
        store.Set(name, value);
        store.Save();
    }

    [Theory]
    [InlineData("adls-prod", "MIDDEN_SECRET_ADLS_PROD")]
    [InlineData("adls prod", "MIDDEN_SECRET_ADLS_PROD")]
    [InlineData("Adls.Prod", "MIDDEN_SECRET_ADLS_PROD")]
    [InlineData("simple", "MIDDEN_SECRET_SIMPLE")]
    public void GetEnvironmentVariableName_NormalizesName(string secretName, string expected) =>
        Assert.Equal(expected, SecretResolver.GetEnvironmentVariableName(secretName));

    [Fact]
    public void Resolve_NullOrWhitespace_ReturnsNotProvided()
    {
        using var sut = CreateResolver();

        Assert.Equal(SecretSource.NotProvided, sut.Resolve(null).Source);
        Assert.Equal(SecretSource.NotProvided, sut.Resolve("   ").Source);
    }

    [Fact]
    public void Resolve_PlainValue_IsTreatedAsLiteralForBackwardsCompatibility()
    {
        using var sut = CreateResolver();

        var actual = sut.Resolve("an-inline-secret");

        Assert.Equal(SecretSource.Literal, actual.Source);
        Assert.Equal("an-inline-secret", actual.Value);
    }

    [Fact]
    public void Resolve_Reference_PrefersEnvironmentVariableOverStore()
    {
        GivenStoredSecret("adls-prod", "from-store");
        SetEnvironmentVariable("MIDDEN_SECRET_ADLS_PROD", "from-environment");

        using var sut = CreateResolver();
        var actual = sut.Resolve("secret:adls-prod");

        Assert.Equal(SecretSource.EnvironmentVariable, actual.Source);
        Assert.Equal("from-environment", actual.Value);
    }

    [Fact]
    public void Resolve_Reference_FallsBackToStore()
    {
        GivenStoredSecret("adls-prod", "from-store");

        using var sut = CreateResolver();
        var actual = sut.Resolve("secret:adls-prod");

        Assert.Equal(SecretSource.SecretStore, actual.Source);
        Assert.Equal("from-store", actual.Value);
    }

    [Fact]
    public void Resolve_UnknownReference_ThrowsWithActionableMessage()
    {
        using var sut = CreateResolver();

        var exception = Assert.Throws<InvalidOperationException>(() => sut.Resolve("secret:missing"));

        Assert.Contains("midden secret set missing", exception.Message, StringComparison.Ordinal);
        Assert.Contains("MIDDEN_SECRET_MISSING", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_NoSecretReferences_NeverOpensTheStore()
    {
        // The store is opened lazily so a configuration without secrets never prompts for a password.
        using var sut = new SecretResolver(StorePath, () => throw new InvalidOperationException("Should not prompt."));

        Assert.Equal(SecretSource.Literal, sut.Resolve("inline").Source);
    }

    [Fact]
    public void Resolve_EmptyReferenceName_Throws()
    {
        using var sut = CreateResolver();

        Assert.Throws<InvalidOperationException>(() => sut.Resolve("secret:"));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var name in environmentVariablesToClear)
        {
            Environment.SetEnvironmentVariable(name, null);
        }

        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }
}
