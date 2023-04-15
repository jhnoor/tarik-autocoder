using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public static class FileHelper
{
    public static DirectoryNode? GetTree(string path, string? localDirectory = null, int depth = 32)
    {
        if (depth < 0) return null;

        var node = new DirectoryNode(path, localDirectory ?? path);

        foreach (var directory in Directory.GetDirectories(path))
        {
            if (string.IsNullOrEmpty(directory)) continue;

            if (directory.Contains(".git")) continue;

            node.Directories.Add(GetTree(path: directory, localDirectory: directory, depth - 1)!);
        }

        return node;
    }

    public static List<PathTo> Flatten(this DirectoryNode? node, string localDirectory)
    {
        if (node == null || node.Files.Count == 0) return new List<PathTo>();

        var paths = node.Files;

        foreach (var directory in node.Directories)
        {
            node.Files.AddRange(directory.Flatten(localDirectory));
        }

        return paths;
    }
}

