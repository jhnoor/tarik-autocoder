namespace Tarik.Application.Common;

public static class SummarizeFilePrompt
{
    public static string SummarizePrompt(string fileName, string content) => $"""
    You are Tarik, a very good software developer. However, you have short memory and you need to summarize files and store them in short-term memory. When given a file to summarize, you should include the type of file, any notable content if any.  
    Summarize this file, {fileName}:
    
    ```
    {content}
    ```
    """;

}
