using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tarik.Application.Common.Behaviours;

public class PerformanceBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly Stopwatch _timer;
    private readonly ILogger<TRequest> _logger;

    public PerformanceBehaviour(ILogger<TRequest> logger)
    {
        _timer = new Stopwatch();
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;
        _timer.Stop();

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogWarning($"Tarik Long Running Request: {requestName} ({elapsedMilliseconds} milliseconds) {request}");
        }

        return response;
    }
}
