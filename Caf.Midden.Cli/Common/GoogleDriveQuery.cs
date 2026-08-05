namespace Caf.Midden.Cli.Common;

/// <summary>
/// Google Drive's <c>files.list</c> <c>q</c> parameter is a small query language in which
/// backslash and single-quote are syntactically significant. A search term containing either
/// character (for example an apostrophe in a file or folder name) must be escaped before being
/// interpolated into the query string, otherwise it breaks the query or silently changes which
/// files are matched.
/// </summary>
public static class GoogleDriveQuery
{
    public static string EscapeTerm(string term) =>
        term.Replace("\\", "\\\\").Replace("'", "\\'");
}
