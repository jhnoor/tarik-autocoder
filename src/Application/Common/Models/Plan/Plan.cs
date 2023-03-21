namespace Tarik.Application.Common;

public class Plan
{
    public List<CreateFilePlanStep> CreateFileSteps { get; set; } = new();
    public List<EditFilePlanStep> EditFileSteps { get; set; } = new();
    public List<DeleteFilePlanStep> DeleteFileSteps { get; set; } = new();
}