using Microsoft.Extensions.Logging;
using Polly;

namespace Tarik.Application.Common;

public static class RetryPolicies
{
    public static IAsyncPolicy CreateRetryPolicy(int retries, ILogger logger)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(exception, $"Retry {retryCount} of {retries} in {timeSpan.TotalSeconds} seconds");
                    context["RetryCount"] = retryCount + 1;
                });
    }
}