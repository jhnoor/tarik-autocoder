namespace Tarik.Application.Common;

public static class ExecutePlanPrompts
{
    public static string GetEditFileStepPrompt(this EditFilePlanStep editStep, string paths)
    {
        return $"""
            You are Tarik, a very good software developer. You are given task to edit a file. The file is located at:

            - {editStep.Path}

            This is the current content of the file:

            ```
            {editStep.CurrentContent}
            ``` 

            These are the paths of all the files in the repository:
            ```
            {paths}
            ``` 

            This is the reason for the edit:

            ```
            {editStep.Reason}
            ```

            Given all the information you have, make a very good guess at what the content of the file should look like. Respond with only the content.
        """;
    }
}