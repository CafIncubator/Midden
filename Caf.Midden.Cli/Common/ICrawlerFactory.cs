using Caf.Midden.Cli.Models;

namespace Caf.Midden.Cli.Common;

/// <summary>
/// Creates the crawler for a data store. Exists so that collate orchestration — partial failure
/// handling, <c>--strict</c>, collision reporting, the run summary — can be exercised against
/// fake crawlers instead of requiring a configured cloud account.
/// </summary>
public interface ICrawlerFactory
{
    /// <summary>
    /// Returns a crawler for <paramref name="dataStore"/>, or <c>null</c> when the store does not
    /// carry enough configuration to be crawled. Returning <c>null</c> rather than throwing keeps
    /// "misconfigured" distinct from "failed to connect", which the caller reports differently.
    /// </summary>
    ICrawl? Create(
        DataStore dataStore,
        string? clientSecret,
        string? sharedAccessSignature,
        string googleTokenStorePath);
}
