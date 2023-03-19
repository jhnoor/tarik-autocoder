using MediatR;
using OpenAI.GPT3.Interfaces;
using Tarik.Application.Common.DTOs;

namespace Tarik.Application.CQRS;

public class ImplementWorkCommand : IRequest<Unit>
{
    public ImplementWorkCommand(WorkItemDTO workItem)
    {
        WorkItem = workItem;
    }

    public WorkItemDTO WorkItem { get; }

    public class ImplementWorkCommandHandler : IRequestHandler<ImplementWorkCommand>
    {
        private readonly IOpenAIService _openAIService;

        public ImplementWorkCommandHandler(IOpenAIService openAIService)
        {
            _openAIService = openAIService;
        }

        public async Task<Unit> Handle(ImplementWorkCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}