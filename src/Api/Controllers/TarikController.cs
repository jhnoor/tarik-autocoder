using Microsoft.AspNetCore.Mvc;
using Tarik.Application.Common.DTOs;
using Tarik.Application.CQRS;

namespace Tarik.Api.Controllers;

public class TarikController : ApiControllerBase
{
    [HttpPost("daily-digest")]
    public async Task<IActionResult> GetDailyDigest(GetDailyDigestDTO dailyDigestDTO, CancellationToken cancellationToken)
    {
        var userActivitySummary = await Mediator.Send(new GetUserActivitySummaryQuery(dailyDigestDTO), cancellationToken);

        return Ok(userActivitySummary);
    }

    [HttpPost("smart-daily-digest")]
    public async Task<IActionResult> GetSmartDailyDigest(GetDailyDigestDTO dailyDigestDTO, CancellationToken cancellationToken)
    {
        var userActivitySummary = await Mediator.Send(new GetSmartDailyDigestQuery(dailyDigestDTO), cancellationToken);

        return Ok(userActivitySummary);
    }
}