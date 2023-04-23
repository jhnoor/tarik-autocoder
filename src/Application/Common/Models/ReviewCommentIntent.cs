using System.Text.Json.Serialization;

namespace Tarik.Application.Common;

public class ReviewCommentIntent
{
    public string Intent { get; set; }
    public List<RelevantFile> RelevantFiles { get; set; }

    [JsonConstructor]
    public ReviewCommentIntent(string intent, List<RelevantFile> relevantFiles)
    {
        Intent = intent;
        RelevantFiles = relevantFiles;
    }

    public override string ToString()
    {
        return $"""
            Intent: {Intent}
            RelevantFiles: {string.Join(", ", RelevantFiles)}
        """;
    }
}