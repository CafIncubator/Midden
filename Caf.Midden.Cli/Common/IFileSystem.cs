namespace Caf.Midden.Cli.Common;

/// <summary>
/// The file system operations the local crawler actually performs, and no more.
/// <para>
/// Deliberately not a general-purpose file system abstraction. A wide surface would be a
/// maintenance cost with no testing benefit; these five members are exactly what
/// <c>LocalFileSystemCrawler</c> calls, which is enough to walk a faked tree in a unit test
/// without touching a real disk.
/// </para>
/// </summary>
public interface IFileSystem
{
    bool DirectoryExists(string path);

    /// <summary>
    /// Returns the immediate children of <paramref name="path"/>, both files and directories.
    /// The crawler recurses manually so it can skip reparse points, so a recursive variant is
    /// intentionally not offered here.
    /// </summary>
    string[] GetFileSystemEntries(string path);

    FileAttributes GetAttributes(string path);

    string ReadAllText(string path);

    Stream OpenRead(string path);
}
