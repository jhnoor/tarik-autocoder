using System.Text.Json;
using System.Text.RegularExpressions;

namespace Tarik.Application.Common;

public class Plan
{
    private Regex stepByStepDiscussionPattern = new Regex(@"## Step-by-step discussion\s*(.*)\n", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private Regex editMatchPattern = new Regex(@"Edit the file (.*)\s+\|\s+(.*?)\s+\|\s+(\[.*?\])", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private Regex createMatchPattern = new Regex(@"Create and populate the file (.*)\s+\|\s+(.*?)\s+\|\s+(\[.*?\])", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    public string StepByStepDiscussion { get; set; }
    public string WorkItemTitle { get; }
    public string WorkItemBody { get; }
    public List<CreateFilePlanStep> CreateFileSteps { get; set; } = new();
    public List<EditFilePlanStep> EditFileSteps { get; set; } = new();
    public List<DeleteFilePlanStep> DeleteFileSteps { get; set; } = new();

    public Plan(string approvedPlan, string workItemTitle, string workItemBody, string localDirectory)
    {
        WorkItemTitle = workItemTitle;
        WorkItemBody = workItemBody;

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
            var path = match.Groups[1].Value.Trim().TrimStart('/').Trim('`', '"', '\"', '\'');
            var reason = match.Groups[2].Value.Trim();
            var relevantFiles = match.Groups[3].Value;
            List<string> relevantFilePaths = new();

            if (!string.IsNullOrEmpty(relevantFiles) || relevantFiles != "[]")
            {
                relevantFilePaths = relevantFiles.Deserialize<List<string>>() ?? new List<string>();
            }

            var step = new EditFilePlanStep(
                path: path,
                reason: reason,
                localDirectory: localDirectory,
                relevantFilePaths: relevantFilePaths);

            EditFileSteps.Add(step);
        }

        foreach (Match match in createMatches)
        {
            var path = match.Groups[1].Value.Trim().TrimStart('/').Trim('`', '"', '\"', '\'');
            var reason = match.Groups[2].Value.Trim();
            var relevantFiles = match.Groups[3].Value;
            List<string> relevantFilePaths = new();

            if (!string.IsNullOrEmpty(relevantFiles) || relevantFiles != "[]") // TODO code smell
            {
                relevantFilePaths = relevantFiles.Deserialize<List<string>>() ?? new List<string>();
            }
            var step = new CreateFilePlanStep(
                path: path,
                reason: reason,
                localDirectory: localDirectory,
                relevantFilePaths: relevantFilePaths);

            CreateFileSteps.Add(step);
        }

        if (editMatches.Count + createMatches.Count == 0)
        {
            throw new ArgumentException("No valid plan steps found");
        }
    }

    public string Dump() => $@"""
        ## {WorkItemTitle}
        {WorkItemBody}

        ## Step-by-step discussion

        {StepByStepDiscussion}

        ## Create files

        {string.Join("\n", CreateFileSteps.Select(x => x.Dump()))}

        ## Edit files

        {string.Join("\n", EditFileSteps.Select(x => x.Dump()))}
        """;
}
