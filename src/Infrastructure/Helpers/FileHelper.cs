namespace Tarik.Infrastructure;

public static class FileHelper
{
    public static DirectoryNode? GetTree(string path, int depth = 32)
    {
        if (depth < 0) return null;

        var node = new DirectoryNode(path);

        foreach (var directory in Directory.GetDirectories(path))
        {
            if (string.IsNullOrEmpty(directory)) continue;

            if (directory.Contains(".git")) continue;

            node.Directories.Add(GetTree(directory, depth - 1)!);
        }

        return node;

    }

    public static List<string> Flatten(this DirectoryNode? node, string? omitPath = null)
    {
        if (node == null) return new List<string>();

        var paths = new List<string>();

        foreach (var fullFilePath in node.Files)
        {
            var filePath = omitPath == null ? fullFilePath : fullFilePath.Replace(omitPath, string.Empty);
            paths.Add(filePath);
        }

        foreach (var directory in node.Directories)
        {
            paths.AddRange(directory.Flatten(omitPath));
        }

        return paths;
    }
}

