using System.Text.RegularExpressions;

namespace Tarik.Application.Common;

public class Plan
{
    private Regex stepByStepDiscussionPattern = new Regex(@"## Step-by-step discussion\s*(.*)\n", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private Regex editMatchPattern = new Regex(@"\d+\.\s*Edit the file\s*([^|]+)\s\|\s*(.*)\n", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private Regex createMatchPattern = new Regex(@"\d+\.\s*Create and populate the file\s*([^|]+)\s\|\s*(.*)\n", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    public string StepByStepDiscussion { get; set; }
    public List<CreateFilePlanStep> CreateFileSteps { get; set; } = new();
    public List<EditFilePlanStep> EditFileSteps { get; set; } = new();
    public List<DeleteFilePlanStep> DeleteFileSteps { get; set; } = new();

    public Plan(string approvedPlan, string localDirectory)
    {
        var stepByStepDiscussionMatches = stepByStepDiscussionPattern.Matches(approvedPlan);
        var editMatches = editMatchPattern.Matches(approvedPlan);
        var createMatches = createMatchPattern.Matches(approvedPlan);

        if (stepByStepDiscussionMatches.Count == 0)
        {
            throw new ArgumentException("No step-by-step discussion found");
        }

        StepByStepDiscussion = stepByStepDiscussionMatches[0].Groups[1].Value.Trim();

        foreach (Match match in editMatches)
        {
            var step = new EditFilePlanStep(
                path: match.Groups[1].Value.Trim().TrimStart('/').Trim('`', '"', '\"', '\''),
                reason: match.Groups[2].Value.Trim(),
                localDirectory: localDirectory);

            EditFileSteps.Add(step);
        }

        foreach (Match match in createMatches)
        {
            var step = new CreateFilePlanStep(
                path: match.Groups[1].Value.Trim().TrimStart('/').Trim('`', '"', '\"', '\''),
                reason: match.Groups[2].Value.Trim(),
                localDirectory: localDirectory);

            CreateFileSteps.Add(step);
        }

        if (editMatches.Count + createMatches.Count == 0)
        {
            throw new ArgumentException("No valid plan steps found");
        }
    }
}