namespace Caf.Midden.Cli.Common;

/// <summary>
/// The real file system. A direct pass-through to <see cref="System.IO"/> so that injecting the
/// seam changes nothing about how the crawler behaves against a researcher's actual disk.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    /// <summary>
    /// Shared instance for the common case where no faking is needed. The type is stateless.
    /// </summary>
    public static readonly PhysicalFileSystem Instance = new();

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string[] GetFileSystemEntries(string path) => Directory.GetFileSystemEntries(path);

    public FileAttributes GetAttributes(string path) => File.GetAttributes(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public Stream OpenRead(string path) => File.OpenRead(path);
}
