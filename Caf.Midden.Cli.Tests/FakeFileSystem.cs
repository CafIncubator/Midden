using Caf.Midden.Cli.Common;
using System.Text;

namespace Caf.Midden.Cli.Tests;

/// <summary>
/// An in-memory file system for crawler tests, so a tree shape can be described directly rather
/// than staged on disk. Chiefly this makes cases that require elevation on Windows, such as
/// reparse points, testable on any machine.
/// </summary>
internal sealed class FakeFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> reparsePoints = new(StringComparer.OrdinalIgnoreCase);

    public FakeFileSystem AddDirectory(string path)
    {
        AddAncestors(path);
        directories.Add(path);
        return this;
    }

    public FakeFileSystem AddFile(string path, string contents = "{}")
    {
        AddAncestors(path);
        files[path] = contents;
        return this;
    }

    /// <summary>
    /// Adds a directory flagged as a reparse point, standing in for a symlink or junction that
    /// the crawler must not follow.
    /// </summary>
    public FakeFileSystem AddReparsePointDirectory(string path)
    {
        AddDirectory(path);
        reparsePoints.Add(path);
        return this;
    }

    public bool DirectoryExists(string path) => directories.Contains(path);

    public string[] GetFileSystemEntries(string path) =>
        [.. directories.Concat(files.Keys).Where(entry => IsImmediateChildOf(entry, path)).Order(StringComparer.OrdinalIgnoreCase)];

    public FileAttributes GetAttributes(string path)
    {
        var attributes = directories.Contains(path) ? FileAttributes.Directory : FileAttributes.Normal;

        if (reparsePoints.Contains(path))
        {
            attributes |= FileAttributes.ReparsePoint;
        }

        return attributes;
    }

    public string ReadAllText(string path) =>
        files.TryGetValue(path, out var contents) ? contents : throw new FileNotFoundException(path);

    public Stream OpenRead(string path) => new MemoryStream(Encoding.UTF8.GetBytes(ReadAllText(path)));

    private void AddAncestors(string path)
    {
        var parent = Path.GetDirectoryName(path);

        while (!string.IsNullOrEmpty(parent))
        {
            directories.Add(parent);
            parent = Path.GetDirectoryName(parent);
        }
    }

    private static bool IsImmediateChildOf(string entry, string directory) =>
        string.Equals(Path.GetDirectoryName(entry), directory, StringComparison.OrdinalIgnoreCase);
}
