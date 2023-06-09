namespace Tarik.Application.Common;

public class PathTo
{
    public string RelativePath { get; }
    public string AbsolutePath { get; }

    public PathTo(string absolutePath, string localDirectory)
    {
        AbsolutePath = absolutePath;
        RelativePath = absolutePath.Replace(localDirectory, string.Empty);
    }

    public PathTo(string relativePath, string localDirectory, bool isRelative)
    {
        RelativePath = relativePath;
        AbsolutePath = Path.Combine(localDirectory, relativePath.TrimStart('/'));
    }
}