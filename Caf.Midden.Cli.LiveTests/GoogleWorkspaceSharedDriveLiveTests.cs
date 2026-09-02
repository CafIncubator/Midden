using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Services;

namespace Caf.Midden.Cli.LiveTests;

[Collection(LiveCloudTestCollection.Name)]
[Trait("Category", "LiveIntegration")]
[Trait("Provider", "GoogleWorkspace")]
public class GoogleWorkspaceSharedDriveLiveTests
{
    private const string OAuthConfigPath = "Assets/CliConfigurationSecrets/GoogleWorkspaceSharedDriveProjectTest.json";
    private const string ServiceAccountConfigPath = "Assets/CliConfigurationSecrets/GoogleWorkspaceSharedDriveProjectTestWithServiceAccount.json";

    [Fact(Explicit = true)]
    public void GetProjects_WithOAuth_ReturnsProjects()
    {
        var dataStore = LiveTestConfiguration.GetDataStoreOrSkip(OAuthConfigPath);
        using var sut = new GoogleWorkspaceSharedDriveCrawler(
            dataStore.ClientId!,
            dataStore.ClientSecret!,
            dataStore.ApplicationName!);

        var actual = sut.GetProjects(new ProjectReader(new ProjectParser()));

        Assert.NotEmpty(actual);
    }

    [Fact(Explicit = true)]
    public void GetProjects_WithServiceAccount_ReturnsProjects()
    {
        var dataStore = LiveTestConfiguration.GetDataStoreOrSkip(ServiceAccountConfigPath);
        LiveTestConfiguration.RequireFileOrSkip(dataStore.AuthFilePath, "Google service account key");
        using var sut = new GoogleWorkspaceSharedDriveCrawler(
            dataStore.AuthFilePath!,
            dataStore.ApplicationName!);

        var actual = sut.GetProjects(new ProjectReader(new ProjectParser()));

        Assert.NotEmpty(actual);
    }
}