namespace Tarik.Application.Common.DTOs;

public class GetDailyDigestDTO
{
    public string? PersonalAccessToken { get; set; }
    /// <summary>
    /// The Azure DevOps URL, e.g. https://dev.azure.com/MyCompany
    /// Must include organization name.
    /// </summary>
    public string? AzureDevOpsUrl { get; set; }
    public string? ProjectName { get; set; }
    public string? UserEmail { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}