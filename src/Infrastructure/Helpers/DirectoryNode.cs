using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class DirectoryNode
{
    public string Path { get; }
    public List<DirectoryNode> Directories { get; }
    public List<PathTo> Files { get; }

    public DirectoryNode(string path, string localDirectory)
    {
        Path = path;
        Directories = new List<DirectoryNode>();
        Files = Directory.GetFiles(path).Select(x => new PathTo(x, localDirectory)).ToList();
    }
}
