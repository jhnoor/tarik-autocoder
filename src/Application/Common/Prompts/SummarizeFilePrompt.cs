namespace Tarik.Application.Common;

public static class SummarizeFilePrompt
{
    public static string SummarizePrompt(string fileName, string content) => $"""
    Summarize this file, {fileName}:
    
    ```
    {content}
    ```
    """;

}
