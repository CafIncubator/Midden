namespace Caf.Midden.Cli.Common;

/// <summary>
/// Minimal logging seam for the crawler service layer.
/// <para>
/// Deliberately thin: the crawlers only ever report progress or a skipped file, so a full
/// structured-logging abstraction would add configuration surface without helping a researcher
/// read the output. This exists so crawler output is capturable in tests and has one place to
/// be routed, rather than being written straight to <see cref="Console"/> from five classes.
/// </para>
/// </summary>
public interface ICrawlLogger
{
    /// <summary>
    /// Reports normal progress, such as how many files a listing found.
    /// </summary>
    void Info(string message);

    /// <summary>
    /// Reports a recoverable problem, such as a file that was skipped because it could not be
    /// parsed. The crawl continues.
    /// </summary>
    void Warning(string message);
}
