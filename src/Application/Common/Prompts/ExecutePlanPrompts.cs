namespace Tarik.Application.Common;

public static class ExecutePlanPrompts
{
    public static async Task<string> GetEditFileStepPrompt(this EditFilePlanStep editStep, Plan plan, IFileService fileService, string shortTermMemory, CancellationToken cancellationToken)
    {
        return $$"""
            You are Tarik, a very good software developer. You are working on this task:

            <plan>
            {{plan.Dump()}}
            </plan>
            
            You are currently working on part of this task, this part you will edit an existing file. The file is located here:

            - {{editStep.PathTo.RelativePath}}

            This is the current content of the file:

            ```
            {{editStep.CurrentContent}}
            ``` 

            For added context, here is your short term memory.
            ```
            {{shortTermMemory}}
            ``` 
            {{await fileService.DumpFiles(editStep.RelevantFiles, cancellationToken)}}

            This is the reason for the edit:

            ```
            {{editStep.Reason}}
            ```

            You must respond with a json, with this structure:

            {"content": "<CONTENT>", "relevantFiles": [{"path": "/path/to/file", "reason":"<reason>"}]}

            relevantFiles is a list of objects with path, and reason.
            Where path is the relevant file path, and reason is the reason why it is impacted by this change.
        """;
    }

    public static async Task<string> GetCreateFileStepPrompt(this CreateFilePlanStep createStep, Plan plan, IFileService fileService, string shortTermMemory, CancellationToken cancellationToken)
    {
        return $$"""
            You are Tarik, a very good software developer. You are working on this task:

            <plan>
            {{plan.Dump()}}
            </plan>

            You are currently working on part of this task, this part you will write a file. The file is located here:

            - {{createStep.PathTo.RelativePath}}

            For added context, here is your short term memory.
            <shortTermMemory>
            {{shortTermMemory}}
            </shortTermMemory>

            {{await fileService.DumpFiles(createStep.RelevantFiles, cancellationToken)}}

            This is the reason you are writing this file for this current step:

            <reason>
            {{createStep.Reason}}
            </reason>

            You must respond with a json, with this structure:

            {"content": "<CONTENT>", "relevantFiles": [{"path": "/path/to/file", "reason":"<reason>"}]}

            If the current content is satisfactory, <CONTENT> should be empty. 
            relevantFiles is a list of objects with path, and reason.
            Where path is the relevant file path, and reason is the reason why it is impacted by this change.
            If no changes are made and <CONTENT> is empty, the relevantFiles list should be empty.
        """;
    }
}