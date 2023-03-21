namespace Tarik.Application.Common;

public class Plan
{
    public List<PlanStep> CreateFileSteps { get; set; } = new();
    public List<PlanStep> EditFileSteps { get; set; } = new();
    public List<PlanStep> DeleteFileSteps { get; set; } = new();
}