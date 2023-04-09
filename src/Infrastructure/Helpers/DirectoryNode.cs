namespace Tarik.Infrastructure;

public class DirectoryNode
{
    public string Path { get; }
    public List<DirectoryNode> Directories { get; }
    public List<string> Files { get; }

    public DirectoryNode(string path)
    {
        Path = path;
        Directories = new List<DirectoryNode>();
        Files = new List<string>();
    }
}
