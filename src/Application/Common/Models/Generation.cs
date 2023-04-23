using System.Text.Json.Serialization;

namespace Tarik.Application.Common;

public class Generation
{
    public string Content { get; set; }
    public List<RelevantFile> RelevantFiles { get; set; }

    public Generation(string content, List<RelevantFile> relevantFiles)
    {
        Content = content;
        RelevantFiles = relevantFiles;
    }

    public override string ToString()
    {
        return $"""
            content:
            {Content}

            relevant_files:
                {string.Join(Environment.NewLine, "- ", RelevantFiles)}
        """;
    }
}
