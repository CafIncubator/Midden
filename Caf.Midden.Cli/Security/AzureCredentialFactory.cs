using Azure.Core;
using Azure.Identity;

namespace Caf.Midden.Cli.Security;

/// <summary>
/// Builds Azure credentials for data stores, preferring identities that require no secret on disk.
/// </summary>
public static class AzureCredentialFactory
{
    /// <summary>
    /// Set to any non-empty value to suppress interactive sign in, for example in a scheduled job.
    /// </summary>
    public const string NonInteractiveEnvironmentVariable = "MIDDEN_NON_INTERACTIVE";

    /// <summary>
    /// True when the CLI may open a browser for sign in. Automation such as an Azure App Service
    /// cron job runs with redirected input, where an interactive prompt would hang until timeout.
    /// </summary>
    public static bool IsInteractiveAllowed =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable(NonInteractiveEnvironmentVariable))
        && !Console.IsInputRedirected;

    /// <summary>
    /// Creates a credential that tries, in order: environment variables, managed identity, an
    /// existing developer sign in, and finally an interactive browser sign in when permitted.
    /// <para>
    /// This lets a researcher authenticate with their normal institutional account and lets a
    /// deployed job use managed identity, in both cases without a client secret in the configuration.
    /// </para>
    /// </summary>
    public static TokenCredential CreateDefaultCredential(string? tenantId) =>
        CreateDefaultCredential(tenantId, authorityHost: null);

    /// <summary>
    /// Overload accepting an explicit Azure AD authority host, for Azure Government, Azure China,
    /// or other sovereign clouds where the public <c>login.microsoftonline.com</c> authority does
    /// not apply.
    /// </summary>
    public static TokenCredential CreateDefaultCredential(string? tenantId, string? authorityHost)
    {
        var options = new DefaultAzureCredentialOptions
        {
            ExcludeInteractiveBrowserCredential = !IsInteractiveAllowed,
        };

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            options.TenantId = tenantId;
            options.InteractiveBrowserTenantId = tenantId;
            options.SharedTokenCacheTenantId = tenantId;
            options.VisualStudioTenantId = tenantId;
        }

        if (!string.IsNullOrWhiteSpace(authorityHost))
        {
            options.AuthorityHost = new Uri(authorityHost);
        }

        return new DefaultAzureCredential(options);
    }
}
