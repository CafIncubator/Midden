using System.Text.RegularExpressions;

namespace Caf.Midden.Cli.Common;

/// <summary>
/// Cloud SDK exceptions (Azure in particular) often interpolate the request URI directly into
/// <see cref="Exception.Message"/>, including the query string. For a SAS-authenticated store
/// that query string *is* the credential, so it must never reach the console by default.
/// </summary>
public static class ExceptionSanitizer
{
    private static readonly Regex QueryStringPattern = new(@"\?[^\s""'>]*", RegexOptions.Compiled);

    /// <summary>
    /// Returns a message safe to print. Without <paramref name="verbose"/>, any query string
    /// (which may carry a SAS token or other credential material) is replaced with a redaction
    /// marker. With <paramref name="verbose"/>, the full exception (including stack trace) is
    /// returned for troubleshooting.
    /// </summary>
    public static string Describe(Exception exception, bool verbose) =>
        verbose ? exception.ToString() : QueryStringPattern.Replace(exception.Message, "?<redacted>");
}
