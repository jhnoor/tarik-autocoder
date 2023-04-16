namespace Tarik.Application.Common;

public class RelevantFile
{
    public string Path { get; }
    public string Reason { get; }

    public RelevantFile(string path, string reason)
    {
        Path = path;
        Reason = reason;
    }

    public override string ToString()
    {
        return $"""
            path: {Path}
            reason: {Reason}
        """;
    }
}