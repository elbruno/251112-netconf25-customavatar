using AzureAIAvatarBlazor.Components;
using AzureAIAvatarBlazor.Services;
using AzureAIAvatarBlazor.Services.Caching;
using AzureAIAvatarBlazor.MAFFoundry;
using AzureAIAvatarBlazor.MAFLocal;
using AzureAIAvatarBlazor.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Serilog;
using Serilog.Events;

// ==============================
// Configure Serilog First
// ==============================

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Azure AI Avatar Blazor application");

    var builder = WebApplication.CreateBuilder(args);

    // Add user secrets support for development before building final logger
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>();
    }

    // Build a final Serilog logger from configuration (do not depend on DI services)
    var loggerConfig = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithThreadId()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "AzureAIAvatarBlazor")
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}");

    // If Application Insights connection string present, create a local TelemetryClient and add sink
    string? aiConnection = builder.Configuration.GetConnectionString("appinsights")
                          ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
                          ?? builder.Configuration["ApplicationInsights:ConnectionString"];

    if (!string.IsNullOrWhiteSpace(aiConnection))
    {
        try
        {
            var telemetryConfig = Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.CreateDefault();
            telemetryConfig.ConnectionString = aiConnection;
            var telemetryClient = new Microsoft.ApplicationInsights.TelemetryClient(telemetryConfig);

            loggerConfig.WriteTo.ApplicationInsights(
                telemetryClient,
                TelemetryConverter.Traces,
                restrictedToMinimumLevel: LogEventLevel.Information);
        }
        catch
        {
            // ignore and continue without AI sink
        }
    }

    var logger = loggerConfig.CreateLogger();

    // Replace the global logger with the final logger and register it with the host without using the services-callback
    Log.Logger = logger;
    builder.Host.UseSerilog();

    builder.AddServiceDefaults();

    // ==============================
    // Redis Caching
    // ==============================

    // Add Redis connection from Aspire
    builder.AddRedisClient("cache");

    // ==============================
    // MAF Foundry Integration
    // ==============================

    // Register MAF Foundry agents and chat client
    // This will configure IChatClient and IEmbeddingGenerator if Microsoft Foundry endpoint is available
    builder.AddMAFFoundryAgents();

    // ==============================
    // MAF Local Integration
    // ==============================

    // Register MAF Local agent provider for Agent-LLM mode
    // This uses the IChatClient registered by MAFFoundry to create agents on-demand
    builder.AddMAFLocalAgents();

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Register application services
    builder.Services.AddScoped<AzureAIAgentService>();
    builder.Services.AddScoped<ConfigurationService>();
    builder.Services.AddSingleton<TelemetryService>();

    // Register caching service
    builder.Services.AddSingleton<ICachingService, RedisCachingService>();

    // ==============================
    // Health Checks
    // ==============================

    builder.Services.AddHealthChecks()
        .AddCheck<RedisHealthCheck>(
            "redis",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "cache" })
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

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
        };
    });

    app.Run();

    Log.Information("Azure AI Avatar Blazor application stopped");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
