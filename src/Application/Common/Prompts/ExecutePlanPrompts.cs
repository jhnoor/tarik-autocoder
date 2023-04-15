namespace Tarik.Application.Common;

public static class ExecutePlanPrompts
{
    public static async Task<string> GetEditFileStepPrompt(this EditFilePlanStep editStep, Plan plan, IFileService fileService, string shortTermMemory, CancellationToken cancellationToken)
    {
        return $"""
            You are Tarik, a very good software developer. You are working on this task:

            <plan>
            {plan.Dump()}
            </plan>
            
            You are currently working on part of this task, this part you will edit an existing file. The file is located here:

            - {editStep.PathTo.RelativePath}

            This is the current content of the file:

            ```
            {editStep.CurrentContent}
            ``` 

            For added context, here is your short term memory.
            ```
            {shortTermMemory}
            ``` 

            {await fileService.DumpRelevantFiles(editStep, cancellationToken)}

            This is the reason for the edit:

            ```
            {editStep.Reason}
            ```

            Respond with only the content of the file, do not format it as a code block.
        """;
    }

    public static async Task<string> GetCreateFileStepPrompt(this CreateFilePlanStep createStep, Plan plan, IFileService fileService, string shortTermMemory, CancellationToken cancellationToken)
    {
        return $"""
            You are Tarik, a very good software developer. You are working on this task:

            <plan>
            {plan.Dump()}
            </plan>

            You are currently working on part of this task, this part you will write a file. The file is located here:

            - {createStep.PathTo.RelativePath}

            For added context, here is your short term memory.
            <shortTermMemory>
            {shortTermMemory}
            </shortTermMemory>

            {await fileService.DumpRelevantFiles(createStep, cancellationToken)}

            This is the reason you are writing this file for this current step:

            <reason>
            {createStep.Reason}
            </reason>

            Respond with only the content of the file, do not format it as a code block.
        """;
    }

    private static async Task<string> DumpRelevantFiles(this IFileService fileService, MutateFilePlanStep mutateStep, CancellationToken cancellationToken)
    {
        if (mutateStep.RelevantFiles.Count == 0)
        {
            return "";
        }

        var relevantFiles = await Task.WhenAll(mutateStep.RelevantFiles.Select(file => fileService.GetFileContent(file, cancellationToken)));
        var relevantFilesDump = relevantFiles.Select((content, index) => $"""
            - {mutateStep.RelevantFiles[index].RelativePath}

            ```
            {content}
            ```
        """);

        return $"""
            For added context, here are the relevant files:

            <relevantFiles>
            {string.Join("", relevantFilesDump)}
            </relevantFiles>
        """;
    }
}