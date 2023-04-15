namespace Tarik.Application.Common;

public static class ExecutePlanPrompts
{
    public static string GetEditFileStepPrompt(this EditFilePlanStep editStep, string stepByStepDiscussion, string shortTermMemory)
    {
        return $"""
            You are Tarik, a very good software developer. You are working on this task:

            ```
            {stepByStepDiscussion}
            ```
            
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

            This is the reason for the edit:

            ```
            {editStep.Reason}
            ```

            Respond with only the content of the file, do not format it as a code block.
        """;
    }

    public static string GetCreateFileStepPrompt(this CreateFilePlanStep createStep, string stepByStepDiscussion, string shortTermMemory)
    {
        return $"""
            You are Tarik, a very good software developer. You are working on this task:

            ```
            {stepByStepDiscussion}
            ```

            You are currently working on part of this task, this part you will write a file. The file is located here:

            - {createStep.PathTo.RelativePath}

            For added context, here is your short term memory.
            ```
            {shortTermMemory}
            ``` 

            This is the reason you are writing this file for this current step:

            ```
            {createStep.Reason}
            ```

            Respond with only the content of the file, do not format it as a code block.
        """;
    }
}