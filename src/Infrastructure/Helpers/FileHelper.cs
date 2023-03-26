using System.Text;

namespace Tarik.Infrastructure;

public static class FileHelper
{
    public static DirectoryNode? GetTree(string path, int depth = 32)
    {
        if (depth < 0) return null;

        var node = new DirectoryNode
        {
            Path = path,
            Directories = new List<DirectoryNode>(),
            Files = new List<string>(Directory.GetFiles(path))
        };

        foreach (var directory in Directory.GetDirectories(path))
        {
            if (string.IsNullOrEmpty(directory)) continue;

            if (directory.Contains(".git")) continue;

            node.Directories.Add(GetTree(directory, depth - 1)!);
        }

        return node;

    }

    public static string SerializePaths(this DirectoryNode? node, string omitPath)
    {
        if (node == null) return string.Empty;

        var sb = new StringBuilder();

        foreach (var file in node.Files)
        {
            var filePath = file.Replace(omitPath, string.Empty);
            sb.AppendLine(filePath);
        }

        foreach (var directory in node.Directories)
        {
            sb.AppendLine(directory.SerializePaths(omitPath));
        }

        return sb.ToString();
    }
}

