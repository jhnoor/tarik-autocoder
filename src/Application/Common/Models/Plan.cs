using System.Text.RegularExpressions;

namespace Tarik.Application.Common;

public class Plan
{
    public List<CreateFilePlanStep> CreateFileSteps { get; set; } = new();
    public List<EditFilePlanStep> EditFileSteps { get; set; } = new();
    public List<DeleteFilePlanStep> DeleteFileSteps { get; set; } = new();

    public Plan(string approvedPlan)
    {
        string editMatchPattern = @"\d+\.\s*Edit the file\s*([^|]+)\s\|\s*(.*)\n";
        string createMatchPattern = @"\d+\.\s*Create a new file\s*([^|]+)\s\|\s*(.*)\n";

        var editMatches = Regex.Matches(approvedPlan, editMatchPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline, TimeSpan.FromSeconds(1));
        var createMatches = Regex.Matches(approvedPlan, createMatchPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline, TimeSpan.FromSeconds(1));

        foreach (Match match in editMatches)
        {
            var step = new EditFilePlanStep
            {
                Path = match.Groups[1].Value.Trim().TrimStart('/'),
                Reason = match.Groups[2].Value.Trim()
            };

            EditFileSteps.Add(step);
        }

        foreach (Match match in createMatches)
        {
            var step = new CreateFilePlanStep
            {
                Path = match.Groups[1].Value.Trim().TrimStart('/'),
                Reason = match.Groups[2].Value.Trim()
            };

            CreateFileSteps.Add(step);
        }

        if (editMatches.Count + createMatches.Count == 0)
        {
            throw new ArgumentException("No valid plan steps found");
        }
    }
}