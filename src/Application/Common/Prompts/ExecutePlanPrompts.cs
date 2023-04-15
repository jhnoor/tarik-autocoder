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

            - {editStep.Path}

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

            Given all the information you have, make a very good guess at what the content of the file should look like. Respond with only the content.
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

            - {createStep.Path}

            For added context, here is your short term memory.
            ```
            {shortTermMemory}
            ``` 

            This is the reason you are writing this file for this current step:

            ```
            {createStep.Reason}
            ```

            Given all the information you have, make a very good guess at what the content of the file should look like. Respond with only the content.
        """;
    }
}