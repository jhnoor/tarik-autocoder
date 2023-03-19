using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tarik.Application.IntegrationTests;

[SetUpFixture]
public partial class Testing
{
    private static WebApplicationFactory<Program> _factory = null!;
    private static IConfiguration _configuration = null!;
    private static IServiceScopeFactory _scopeFactory = null!;

    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        _factory = new CustomWebApplicationFactory();
        _scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        _configuration = _factory.Services.GetRequiredService<IConfiguration>();
    }

    [OneTimeTearDown]
    public void RunAfterAnyTests()
    {
    }
}
