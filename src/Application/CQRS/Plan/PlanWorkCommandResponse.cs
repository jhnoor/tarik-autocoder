using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace Tarik.Application.CQRS;

public class PlanWorkCommandResponse
{
    public bool Successful { get; }
    public string? ErrorMessage { get; }
    public string Response { get; }

    public PlanWorkCommandResponse(ChatCompletionCreateResponse chatResponse)
    {
        Successful = chatResponse.Successful;
        ErrorMessage = chatResponse.Error?.Message;
        Response = chatResponse.Choices.First().Message.Content;
    }
}