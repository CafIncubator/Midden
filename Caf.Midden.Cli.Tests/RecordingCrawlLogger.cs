using Caf.Midden.Cli.Common;

namespace Caf.Midden.Cli.Tests;

/// <summary>
/// Captures crawler output in memory so tests can assert on the messages a crawler produces
/// without depending on console redirection.
/// </summary>
internal sealed class RecordingCrawlLogger : ICrawlLogger
{
    public List<string> Infos { get; } = [];

    public List<string> Warnings { get; } = [];

    public void Info(string message) => Infos.Add(message);

    public void Warning(string message) => Warnings.Add(message);
}
