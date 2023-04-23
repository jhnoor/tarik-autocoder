namespace Tarik.Application.Common;

public static class RespondToReviewCommentPrompts
{
    public static async Task<string> StateIntentPrompt(ReviewComment comment, IFileService fileService, string shortTermMemory, CancellationToken cancellationToken)
    {
        return $$"""
            You are Tarik, a very good software developer. 
            You have created the pull request, and you are now responding to a review comment to a file on the pull request.
            Jargon:
            - Short-term memory: An object detailing all the files in the repo and a summary of their contents

            The comment is for the file with this path:

            - {{comment.PathTo(fileService).RelativePath}}

            Here are is the comment:
            ```
            {{comment.Body}}
            ``` 

            This is the current content of the file:

            ```
            {{await fileService.GetFileContent(comment.PathTo(fileService), cancellationToken)}}
            ```

            For added context, here is your short term memory.
            <shortTermMemory>
            {{shortTermMemory}}
            </shortTermMemory>

            Given the comment, the content of the file, and your short term memory, what is your intent?
            You must respond with a json, with this structure:

            {
                "intent": "<INTENT>",
                "relevantFiles": [
                    {
                        "path": "/path/to/file",
                        "reason":"<reason>"
                    }
                ]
            }

            Where <INTENT> is one of the following options:
            - "I do not understand the comment, and I will ask for clarification
            - "I understand the comment, and I will make the change"
            - "I understand the comment, but I do not agree with it, and I will ask for clarification"
            - "I understand the comment, but I do not agree with it, and I will make the change"
            - "I understand the comment, and it is asking for clarification, I will respond to the comment"

            and relevantFiles is a list of objects with path, and reason.
            Where path is the relevant file path, and reason is the reason why it needs to be changed.
        """;
    }
}