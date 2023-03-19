using System.Text.Json;
using MediatR;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Tarik.Application.Common;
using Tarik.Application.Common.DTOs;

namespace Tarik.Application.CQRS;

public class GetSmartDailyDigestQuery : IRequest<SmartUserActivitySummaryDTO>
{
    public GetSmartDailyDigestQuery(GetDailyDigestDTO dailyDigestDTO)
    {
        DailyDigestDTO = dailyDigestDTO;
    }

    public GetDailyDigestDTO DailyDigestDTO { get; }

    public class GetSmartDailyDigestQueryHandler : IRequestHandler<GetSmartDailyDigestQuery, SmartUserActivitySummaryDTO>
    {
        private readonly IOpenAIService _openAIService;
        private readonly ISender _mediator;
        private readonly IDateTimeService _dateTimeService;

        public GetSmartDailyDigestQueryHandler(IOpenAIService openAIService, ISender mediator, IDateTimeService dateTimeService)
        {
            _openAIService = openAIService;
            _mediator = mediator;
            _dateTimeService = dateTimeService;
        }

        public async Task<SmartUserActivitySummaryDTO> Handle(GetSmartDailyDigestQuery request, CancellationToken cancellationToken)
        {
            var userActivitySummary = await _mediator.Send(new GetUserActivitySummaryQuery(request.DailyDigestDTO), cancellationToken);
            var serializedSummary = JsonSerializer.Serialize(userActivitySummary);
            // due to token window limitations, we have to truncate the JSON to 4096 characters
            var truncatedSerializedSummary = serializedSummary.Length > 4096 ? serializedSummary.Substring(0, 4096) : serializedSummary;

            ChatCompletionCreateRequest chatCompletionCreateRequest = new()
            {
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 500,
                Temperature = 0.1f,
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(@"
You are a very very smart Azure DevOps work summarizer. You generate concise and smart summarizations of a users contributions.

You will receive a JSON string, and will treat them like this: 

* Always start with '## Work status [fromDate] to [toDate]'
* Ignore 'gitCommits'
* Within a section called 'Work Items', create bulletpoints for 'workItemsUpdates', prefix with an emoji for the different states
* Within a section called 'Pull Requests', create hyperlinked bulletpoints for 'pullRequests'

Finally end with a '## Next steps' section, where you suggest what the user should do next.

Remember to format your output in markdown, and use emojis 😎"),
                    ChatMessage.FromUser(truncatedSerializedSummary)
                }
            };

            var completionResult = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionCreateRequest, cancellationToken: cancellationToken);

            if (!completionResult.Successful)
            {
                throw new HttpRequestException($"OpenAI GPT API call failed, error: {completionResult.Error?.Message}");
            }

            return new SmartUserActivitySummaryDTO(completionResult.Choices.First().Message.Content, userActivitySummary);
        }
    }
}