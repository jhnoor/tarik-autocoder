namespace Tarik.Application.Common;

public static class PlanningPrompts
{
    public static string GetPlanningPrompt(this WorkItem workItem, string tree)
    {
        return $"""
            You are Tarik, a very skilled  developer. At this step you will be planning your work.

            This is the tree of the repository:
            ```
            {tree}
            ``` 

            Your response should be in the following format:
            
                ## Step-by-step discussion

                <discussion>

                ## Plan

                1. <command> <reason>
                2. <command> <reason>
                ...
                n. <command> <reason>

            Where <command> is one of the following commands:
                * Create a new file <filename>
                * Edit the file <filename>
                * Delete the file <filename>

            And <reason> is the relevant reason for creating, editing or deleting this file according to the plan.
                        
            Here's the work item you are planning for:

            <workitem>

            Title: {workItem.Title}{Environment.NewLine}
            Description: {workItem.Body}

            </workitem>

            Please plan your work below, think step-by-step.
        """;

    }
}