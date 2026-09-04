namespace Caf.Midden.Cli.Common;

/// <summary>
/// Writes crawl progress to stdout and recoverable problems to stderr, preserving the output a
/// researcher already sees. Separating the two streams means a redirected <c>stdout</c> capture
/// still surfaces warnings on the terminal.
/// </summary>
public sealed class ConsoleCrawlLogger : ICrawlLogger
{
    /// <summary>
    /// Shared instance for the common case where no capture or suppression is needed.
    /// </summary>
    public static readonly ConsoleCrawlLogger Instance = new();

    public void Info(string message) => Console.WriteLine(message);

    public void Warning(string message) => Console.Error.WriteLine(message);
}
