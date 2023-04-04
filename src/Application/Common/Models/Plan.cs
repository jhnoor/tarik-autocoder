using System.Text.RegularExpressions;

namespace Tarik.Application.Common;

public class Plan
{
    public string StepByStepDiscussion { get; set; }
    public List<CreateFilePlanStep> CreateFileSteps { get; set; } = new();
    public List<EditFilePlanStep> EditFileSteps { get; set; } = new();
    public List<DeleteFilePlanStep> DeleteFileSteps { get; set; } = new();

    public Plan(string approvedPlan)
    {
        string stepByStepDiscussionPattern = @"## Step-by-step discussion\s*(.*)\n";
        string editMatchPattern = @"\d+\.\s*Edit the file\s*([^|]+)\s\|\s*(.*)\n";
        string createMatchPattern = @"\d+\.\s*Create and populate the file\s*([^|]+)\s\|\s*(.*)\n";

        var stepByStepDiscussionMatches = Regex.Matches(approvedPlan, stepByStepDiscussionPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline, TimeSpan.FromSeconds(1));
        var editMatches = Regex.Matches(approvedPlan, editMatchPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline, TimeSpan.FromSeconds(1));
        var createMatches = Regex.Matches(approvedPlan, createMatchPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline, TimeSpan.FromSeconds(1));

        if (stepByStepDiscussionMatches.Count == 0)
        {
            throw new ArgumentException("No step-by-step discussion found");
        }

        StepByStepDiscussion = stepByStepDiscussionMatches[0].Groups[1].Value.Trim();

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