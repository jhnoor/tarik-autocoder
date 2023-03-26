namespace Tarik.Infrastructure;

public class DirectoryNode
{
    public string Path { get; set; }
    public List<DirectoryNode> Directories { get; set; }
    public List<string> Files { get; set; }
}
