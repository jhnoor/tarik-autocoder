namespace Tarik.Application.Common;

public class FileData
{
    public string FileHash { get; }
    public string Text { get; }

    public FileData(string fileHash, string text)
    {
        FileHash = fileHash;
        Text = text;
    }
}
