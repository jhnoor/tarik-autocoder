namespace Tarik.Application.Common;

public static class PlanningPrompts
{
    public static string GetPlanningPrompt(this WorkItem workItem, string shortTermMemory)
    {
        return $"""
            You are Tarik, a very skilled  developer. At this step you will be planning your work.

            This is your short-term memory of this repository:
            ```
            {shortTermMemory}
            ``` 

            Your response should be in the following format:
            
                ## Step-by-step discussion

                <discussion>

                ## Plan

                1. <command> | <reason>
                2. <command> | <reason>
                ...
                n. <command> | <reason>

            Where <command> is one of the following commands:
                * "Create and populate the file <path/to/file>"
                * "Edit the file <path/to/file>"
                * "Delete the file <path/to/file>"

            And <reason> is the relevant reason for creating, editing or deleting this file according to the plan. 
            Ensure that the order of the steps is sensible.
                        
            Here's the work item you are planning for:

            <workitem>

            Title: {workItem.Title}{Environment.NewLine}
            Description: {workItem.Body}

            </workitem>

            Please plan your work below, think step-by-step.
        """;

    }
}