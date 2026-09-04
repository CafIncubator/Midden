using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Security;

namespace Caf.Midden.Cli.Services;

/// <summary>
/// Creates the real crawler for a data store based on its configured type and the credentials
/// resolved for it. This is the production implementation of <see cref="ICrawlerFactory"/>; tests
/// substitute a fake so collate orchestration can run without cloud accounts.
/// </summary>
public sealed class CrawlerFactory : ICrawlerFactory
{
    public ICrawl? Create(
        DataStore dataStore,
        string? clientSecret,
        string? sharedAccessSignature,
        string googleTokenStorePath) =>
        dataStore.Type switch
        {
            DataStoreTypes.LocalFileSystem when !string.IsNullOrWhiteSpace(dataStore.Path) =>
                new LocalFileSystemCrawler(dataStore.Path),
            DataStoreTypes.AzureDataLakeGen2
                when !string.IsNullOrWhiteSpace(dataStore.AccountName)
                && !string.IsNullOrWhiteSpace(dataStore.TenantId)
                && !string.IsNullOrWhiteSpace(dataStore.ClientId)
                && !string.IsNullOrWhiteSpace(clientSecret)
                && !string.IsNullOrWhiteSpace(dataStore.AzureFileSystemName) =>
                new AzureDataLakeCrawler(
                    dataStore.AccountName,
                    dataStore.TenantId,
                    dataStore.ClientId,
                    clientSecret,
                    dataStore.AzureFileSystemName,
                    dataStore.AzureEndpointSuffix),

            // No client secret configured: fall back to managed identity, an existing developer
            // sign in, or an interactive browser sign in. This keeps credentials off disk entirely.
            DataStoreTypes.AzureDataLakeGen2
                when !string.IsNullOrWhiteSpace(dataStore.AccountName)
                && !string.IsNullOrWhiteSpace(dataStore.AzureFileSystemName) =>
                CreateAzureDataLakeCrawlerWithDefaultCredential(dataStore),
            DataStoreTypes.GoogleDrive
                when !string.IsNullOrWhiteSpace(dataStore.ClientId)
                && !string.IsNullOrWhiteSpace(clientSecret)
                && !string.IsNullOrWhiteSpace(dataStore.ApplicationName) =>
                new GoogleDriveCrawler(
                    dataStore.ClientId,
                    clientSecret,
                    dataStore.ApplicationName,
                    googleTokenStorePath),
            DataStoreTypes.GoogleWorkspaceSharedDrive
                when !string.IsNullOrWhiteSpace(dataStore.ClientId)
                && !string.IsNullOrWhiteSpace(clientSecret)
                && !string.IsNullOrWhiteSpace(dataStore.ApplicationName) =>
                new GoogleWorkspaceSharedDriveCrawler(
                    dataStore.ClientId,
                    clientSecret,
                    dataStore.ApplicationName,
                    googleTokenStorePath),
            DataStoreTypes.GoogleWorkspaceSharedDrive
                when !string.IsNullOrWhiteSpace(dataStore.AuthFilePath)
                && !string.IsNullOrWhiteSpace(dataStore.ApplicationName) =>
                new GoogleWorkspaceSharedDriveCrawler(
                    dataStore.AuthFilePath,
                    dataStore.ApplicationName),
            DataStoreTypes.AzureFileShares
                when !string.IsNullOrWhiteSpace(dataStore.Uri)
                && !string.IsNullOrWhiteSpace(dataStore.Path)
                && !string.IsNullOrWhiteSpace(sharedAccessSignature) =>
                new AzureFileShareCrawler(
                    dataStore.Uri,
                    dataStore.Path,
                    sharedAccessSignature),
            _ => null,
        };

    private static ICrawl CreateAzureDataLakeCrawlerWithDefaultCredential(DataStore dataStore)
    {
        Console.WriteLine(
            $"No client secret configured for '{dataStore.Name}'. Signing in with managed identity or your Azure account.");

        return new AzureDataLakeCrawler(
            dataStore.AccountName!,
            dataStore.AzureFileSystemName!,
            AzureCredentialFactory.CreateDefaultCredential(dataStore.TenantId, dataStore.AzureAuthorityHost),
            dataStore.AzureEndpointSuffix);
    }
}
