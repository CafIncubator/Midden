using Google.Apis.Http;
using Google.Apis.Util;

namespace Caf.Midden.Cli.Common;

/// <summary>
/// Configures a message handler that retries transient failures shared by both Google Drive
/// crawlers. Without an explicit back-off policy, a transient <c>403 userRateLimitExceeded</c>,
/// <c>429</c>, or <c>5xx</c> response from the Drive API aborts the whole crawl instead of being
/// retried automatically by the underlying HTTP client.
/// </summary>
public static class GoogleDriveServiceFactory
{
    public static void ConfigureRetry(ConfigurableHttpClient httpClient)
    {
        var backOffHandler = new BackOffHandler(new BackOffHandler.Initializer(new ExponentialBackOff()));
        httpClient.MessageHandler.AddUnsuccessfulResponseHandler(backOffHandler);
        httpClient.MessageHandler.AddExceptionHandler(backOffHandler);
    }
}
