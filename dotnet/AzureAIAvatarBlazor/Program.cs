using AzureAIAvatarBlazor.Components;
using AzureAIAvatarBlazor.Services;
using AzureAIAvatarBlazor.MAFFoundry;
using AzureAIAvatarBlazor.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ==============================
// Aspire-Managed Clients
// ==============================

// Note: Speech Service doesn't have Aspire component yet
// Configuration is injected via environment variables from AppHost

// Add user secrets support for development
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// ==============================
// MAF Foundry Integration
// ==============================

// Register MAF Foundry agents and chat client
// This will configure IChatClient and IEmbeddingGenerator if Microsoft Foundry endpoint is available
builder.AddMAFFoundryAgents();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register application services
builder.Services.AddScoped<AzureAIAgentService>();
builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddSingleton<TelemetryService>();

// ==============================
// Health Checks
// ==============================

builder.Services.AddHealthChecks()
    .AddCheck<MicrosoftFoundryHealthCheck>(
        "microsoft_foundry",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready", "foundry" })
    .AddCheck<AzureSpeechHealthCheck>(
        "azure_speech",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready", "speech" })
    .AddCheck<ConfigurationHealthCheck>(
        "configuration",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready", "config" });

var app = builder.Build();

app.MapDefaultEndpoints();

// ==============================
// Health Check Endpoints
// ==============================

// Liveness probe - is the app running?
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false, // Don't run any checks, just return if app is alive
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

// Readiness probe - is the app ready to accept traffic?
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

// Startup probe - has the app finished starting up?
app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = _ => false, // Don't run any checks, startup is complete if we're here
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Endpoint to test connection to Azure OpenAI
app.MapGet("/api/test-openai", async (AzureAIAgentService agentService) =>
{
    var (success, message) = await agentService.TestConnectionAsync();
    return Results.Ok(new { success, message });
});

app.Run();
