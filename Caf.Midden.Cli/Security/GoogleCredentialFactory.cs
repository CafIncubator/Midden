using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;

namespace Caf.Midden.Cli.Security;

/// <summary>
/// Centralizes Google Drive authorization so that the OAuth token cache has a predictable location
/// and so unattended runs fail with a clear message instead of blocking on a browser prompt.
/// </summary>
public static class GoogleCredentialFactory
{
    /// <summary>
    /// Directory holding cached OAuth tokens. Previously this was a relative path named
    /// 'token.json' resolved against the current working directory, which meant the cache location
    /// changed depending on where the CLI happened to be invoked from.
    /// </summary>
    public const string TokenStoreDirectoryName = ".midden-google-tokens";

    private const string UserKey = "user";

    public static readonly string[] Scopes = [DriveService.Scope.DriveReadonly];

    public static string GetTokenStorePath(string configurationDirectory) =>
        Path.Combine(configurationDirectory, TokenStoreDirectoryName);

    public static string GetDefaultTokenStorePath() =>
        GetTokenStorePath(Directory.GetCurrentDirectory());

    /// <summary>
    /// True when a previously cached token exists, meaning authorization can proceed without a
    /// browser prompt.
    /// </summary>
    public static bool HasCachedToken(string tokenStorePath)
    {
        try
        {
            return new FileDataStore(tokenStorePath, fullPath: true)
                .GetAsync<TokenResponse>(UserKey)
                .GetAwaiter()
                .GetResult() is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Authorizes against Google Drive, reusing a cached token when one is available.
    /// </summary>
    /// <param name="allowInteractive">
    /// When false, throws rather than opening a browser if no cached token exists. Scheduled jobs
    /// should pass false so a missing token surfaces as an error instead of a hang.
    /// </param>
    public static UserCredential Authorize(
        string clientId,
        string clientSecret,
        string? tokenStorePath = null,
        bool? allowInteractive = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);

        var resolvedTokenStorePath = string.IsNullOrWhiteSpace(tokenStorePath)
            ? GetDefaultTokenStorePath()
            : Path.GetFullPath(tokenStorePath);

        var interactive = allowInteractive ?? AzureCredentialFactory.IsInteractiveAllowed;

        if (!interactive && !HasCachedToken(resolvedTokenStorePath))
        {
            throw new InvalidOperationException(
                $"No cached Google credentials were found in '{resolvedTokenStorePath}' and interactive sign in is disabled. "
                + "Run 'midden login google' once on a machine with a browser, or configure a service account with 'AuthFilePath'.");
        }

        var clientSecrets = new ClientSecrets
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
        };

        return GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                Scopes,
                UserKey,
                cancellationToken,
                new FileDataStore(resolvedTokenStorePath, fullPath: true))
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Creates a service account credential. This is the correct choice for unattended runs, since
    /// it never requires an interactive sign in and has no token cache.
    /// </summary>
    public static GoogleCredential FromServiceAccountFile(string jsonKeyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonKeyPath);

        if (!File.Exists(jsonKeyPath))
        {
            throw new FileNotFoundException($"Google service account key file '{jsonKeyPath}' does not exist.", jsonKeyPath);
        }

        return CredentialFactory.FromFile(jsonKeyPath, "service_account").CreateScoped(Scopes);
    }
}
