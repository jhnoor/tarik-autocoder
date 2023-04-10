using FluentValidation.AspNetCore;
using NSwag;
using Tarik.Application.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.WebHost.ConfigureKestrel(opt =>
{
    opt.AddServerHeader = false;
});

builder.Services.Configure<AppSettings>(builder.Configuration);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddControllers();

builder.Services.AddRazorPages();
builder.Services.AddSwaggerDocument(config => config.PostProcess = document =>
{
    document.Info.Title = "Tarik API";
    document.Info.Description = "Tarik is an AI-powered bot designed to streamline the software development process by taking on assigned tasks and creating pull requests in relevant code repositories";
    document.Info.Version = "v1";
    document.Info.Contact = new OpenApiContact
    {
        Name = "Jamawadi Noor",
        Email = "jamawadi@gmail.com"
    };
});

var app = builder.Build();

app.UsePathBase("/tarik");

app.UseHealthChecks("/health");
app.UseStaticFiles();

app.UseOpenApi();
app.UseSwaggerUi3(opt =>
{
    opt.Path = "/tarik/api";
    opt.TransformToExternalPath = (internalUiRoute, request) => $"/tarik{internalUiRoute}";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
