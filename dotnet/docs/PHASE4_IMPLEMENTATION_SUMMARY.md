# Phase 4: Structured Logging with Serilog - Implementation Summary

## Overview

Phase 4 replaces the default .NET logging infrastructure with **Serilog** to provide structured logging with rich context, better formatting, and seamless Application Insights integration. This enhances both local development experience and production observability.

## What Was Implemented

### 1. Serilog Infrastructure

#### Packages Added
```xml
<PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
<PackageReference Include="Serilog.Enrichers.Environment" Version="3.0.1" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="4.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
<PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="5.0.0-dev-02322" />
```

#### Bootstrap Logger
Configured **before** WebApplication is built to capture startup errors:

```csharp
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
```

### 2. Main Logger Configuration

Replaces default .NET logging with Serilog:

```csharp
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithThreadId()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "AzureAIAvatarBlazor")
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}")
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<TelemetryClient>(),
            TelemetryConverter.Traces,
            restrictedToMinimumLevel: LogEventLevel.Information);
});
```

### 3. Enrichers

**FromLogContext**: Captures properties added via `LogContext.PushProperty()`  
**ThreadId**: Adds thread ID to all log entries  
**MachineName**: Adds machine/container name  
**EnvironmentName**: Adds environment (Development, Production)  
**Custom Property**: "Application" = "AzureAIAvatarBlazor"

### 4. Sinks

#### Console Sink
- **Local Development**: Rich formatting with colors
- **Output Template**: Shows timestamp, level, source, message, properties
- **Properties Format**: JSON format `{Properties:j}` for easy reading

#### Application Insights Sink
- **Production**: All structured logs sent to Application Insights
- **Minimum Level**: Information (filters out Debug/Verbose)
- **Conversion**: Uses `TelemetryConverter.Traces` for proper trace format

### 5. HTTP Request Logging

Added Serilog request logging middleware:

```csharp
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
```

**Enriched Properties**:
- `RequestHost`: Host header value
- `UserAgent`: Browser/client user agent
- `RemoteIP`: Client IP address

### 6. Structured Logging in Services

#### AzureAIAgentService

**Before** (string interpolation):
```csharp
_logger.LogInformation("Initializing AI Agent with mode: {Mode}", mode);
_logger.LogInformation("Using Endpoint: {Endpoint}", config.Endpoint);
```

**After** (structured properties):
```csharp
_logger.LogInformation("Initializing AI Agent with {AgentMode} mode, Model/Agent: {ModelDeployment}",
    mode, deploymentName);

_logger.LogInformation("Creating LLM-based Agent with Endpoint: {OpenAIEndpoint}, Deployment: {ModelDeployment}",
    config.AzureOpenAI.AgentLLM.Endpoint,
    deploymentName);

_logger.LogError("Unsupported agent mode: {AgentMode}. Supported modes: {SupportedModes}",
    mode, new[] { "Agent-LLM", "Agent-AIFoundry", "Agent-MicrosoftFoundry" });
```

**Structured Properties**:
- `{AgentMode}`: Agent mode (Agent-LLM, Agent-MicrosoftFoundry)
- `{ModelDeployment}`: Model deployment name or agent name
- `{OpenAIEndpoint}`: Azure OpenAI endpoint URL
- `{FoundryEndpoint}`: Microsoft Foundry project endpoint
- `{AgentName}`: Microsoft Foundry agent name
- `{SupportedModes}`: Array of supported modes

#### ConfigurationService

**Before**:
```csharp
_logger.LogInformation("Explicit IsCustomAvatar setting: '{Setting}'", explicitSetting ?? "null");
_logger.LogInformation("Checking character '{Character}' against standard avatars", character);
```

**After**:
```csharp
_logger.LogInformation("Using explicit custom avatar setting: {IsCustomAvatar}, Source: Configuration", result);

_logger.LogInformation("Auto-detected custom avatar: {IsCustomAvatar}, Character: {AvatarCharacter}", isCustom, character);

_logger.LogDebug("Returning in-memory cached configuration (Character: {AvatarCharacter}, IsCustom: {IsCustomAvatar}, UseBuiltInVoice: {UseBuiltInVoice})",
    _cachedConfig.Avatar.Character,
    _cachedConfig.Avatar.IsCustomAvatar,
    _cachedConfig.Avatar.UseBuiltInVoice);
```

**Structured Properties**:
- `{IsCustomAvatar}`: Boolean indicating custom avatar
- `{AvatarCharacter}`: Avatar character name
- `{UseBuiltInVoice}`: Boolean indicating built-in voice usage

**Log Level Changes**:
- Cache hits: Changed from `Information` to `Debug` (reduced noise)

### 7. Graceful Shutdown

Added proper Serilog cleanup:

```csharp
try
{
    Log.Information("Starting Azure AI Avatar Blazor application");
    // ... app code ...
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
```

## Benefits

### Local Development

**Rich Console Output**:
```
[10:30:15 INF] AzureAIAvatarBlazor.Services.AzureAIAgentService
Initializing AI Agent with Agent-MicrosoftFoundry mode, Model/Agent: agent-assistant
{"AgentMode": "Agent-MicrosoftFoundry", "ModelDeployment": "agent-assistant"}

[10:30:15 INF] AzureAIAvatarBlazor.Services.AzureAIAgentService
Creating Microsoft Foundry-based Agent with Endpoint: https://my-project.openai.azure.com, AgentName: agent-assistant
{"FoundryEndpoint": "https://my-project.openai.azure.com", "AgentName": "agent-assistant"}

[10:30:16 INF] HTTP GET /health/ready responded 200 in 15.2341 ms
{"RequestHost": "localhost:5173", "UserAgent": "curl/7.68.0", "RemoteIP": "::1"}
```

**Benefits**:
- Colored output (Level colors in console)
- Structured properties visible
- Source context shows which service logged
- Easy to read and debug

### Production (Application Insights)

**Query Structured Logs**:
```kql
traces
| where customDimensions.Application == "AzureAIAvatarBlazor"
| where customDimensions.AgentMode == "Agent-MicrosoftFoundry"
| where message contains "Initializing AI Agent"
| project timestamp, message, 
          agentMode = tostring(customDimensions.AgentMode),
          deployment = tostring(customDimensions.ModelDeployment)
| order by timestamp desc
```

**Query HTTP Requests**:
```kql
traces
| where message startswith "HTTP"
| extend statusCode = toint(customDimensions.StatusCode),
         elapsed = todouble(customDimensions.Elapsed),
         userAgent = tostring(customDimensions.UserAgent)
| where statusCode >= 400
| summarize count() by statusCode, bin(timestamp, 5m)
| render timechart
```

**Benefits**:
- Structured properties are queryable in KQL
- Better log aggregation and filtering
- Correlation with telemetry and traces
- Custom dashboards using structured data

### Consistent Across Environments

**Same Properties Everywhere**:
- Local: Visible in console
- Aspire Dashboard: Visible in Logs tab
- Application Insights: Queryable in traces table

## Log Levels

### Information
- Application lifecycle events (start, stop)
- Agent initialization
- Configuration changes
- Successful operations

### Warning
- Deprecated features
- Fallback scenarios
- Non-critical issues
- Missing optional configuration

### Error
- Configuration errors
- Agent creation failures
- Exceptions
- Service unavailability

### Debug
- Cache hits/misses
- Detailed operation traces
- Development-only information

## Minimum Level Overrides

To reduce noise, some Microsoft namespaces are set to Warning:

```csharp
.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
.MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
```

This filters out:
- ASP.NET Core routing logs
- HTTP client connection logs
- Middleware pipeline logs

## Configuration via appsettings.json

You can override Serilog settings in appsettings.json:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "AzureAIAvatarBlazor.Services": "Debug"
      }
    }
  }
}
```

## Testing Structured Logging

### 1. Run Application Locally

```bash
cd dotnet/AzureAIAvatarBlazor.AppHost
dotnet run
```

### 2. Observe Console Output

You should see structured logs like:
```
[10:30:15 INF] Starting Azure AI Avatar Blazor application

[10:30:16 INF] AzureAIAvatarBlazor.Services.ConfigurationService
Auto-detected custom avatar: False, Character: lisa
{"IsCustomAvatar": false, "AvatarCharacter": "lisa"}
```

### 3. Test HTTP Request Logging

```bash
curl http://localhost:5173/health/ready
```

Console shows:
```
[10:30:20 INF] HTTP GET /health/ready responded 200 in 15.2341 ms
{"RequestHost": "localhost:5173", "UserAgent": "curl/7.68.0", "RemoteIP": "::1"}
```

### 4. View in Aspire Dashboard

1. Navigate to `https://localhost:15216` (or check console for actual port)
2. Click on "AzureAIAvatarBlazor" project
3. Go to "Logs" tab
4. See structured logs with properties

### 5. Query in Application Insights (Production)

```kql
traces
| where customDimensions.Application == "AzureAIAvatarBlazor"
| where timestamp > ago(1h)
| project timestamp, message, customDimensions
| order by timestamp desc
```

## Common Patterns

### Add Context to Log Scope

```csharp
using (LogContext.PushProperty("SessionId", sessionId))
using (LogContext.PushProperty("UserId", userId))
{
    _logger.LogInformation("Processing chat message");
    // All logs within this scope will have SessionId and UserId properties
}
```

### Log with Structured Properties

```csharp
// ❌ Don't: String interpolation
_logger.LogInformation($"Agent mode is {mode}");

// ✅ Do: Structured properties
_logger.LogInformation("Agent mode is {AgentMode}", mode);
```

### Log Exceptions with Context

```csharp
try
{
    await operation();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed for {AgentMode} with {ModelDeployment}",
        mode, deployment);
    throw;
}
```

## Troubleshooting

### Issue: Logs not appearing in console

**Solution**: Check minimum log level in appsettings.json. Ensure it's set to at least `Information`.

### Issue: Application Insights sink not working

**Solution**: Ensure Application Insights is configured:
```csharp
builder.AddServiceDefaults(); // This configures Application Insights
```

The TelemetryClient is required for the Application Insights sink:
```csharp
.WriteTo.ApplicationInsights(
    services.GetRequiredService<TelemetryClient>(),
    TelemetryConverter.Traces)
```

### Issue: Too many logs in production

**Solution**: Increase minimum level for specific namespaces:
```csharp
.MinimumLevel.Override("AzureAIAvatarBlazor.Services.ConfigurationService", LogEventLevel.Warning)
```

Or configure in appsettings.Production.json:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "AzureAIAvatarBlazor": "Warning"
      }
    }
  }
}
```

## Future Enhancements

1. **Seq Sink** (for local development):
   ```csharp
   .WriteTo.Seq("http://localhost:5341")
   ```

2. **File Sink** (for on-premises deployments):
   ```csharp
   .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
   ```

3. **Conditional Enrichers** (environment-specific):
   ```csharp
   if (context.HostingEnvironment.IsProduction())
   {
       configuration.Enrich.WithProperty("ReleaseVersion", Assembly.GetVersion());
   }
   ```

4. **Audit Trail** (separate audit log):
   ```csharp
   .WriteTo.Logger(lc => lc
       .Filter.ByIncludingOnly(Matching.WithProperty("EventType", "Audit"))
       .WriteTo.File("logs/audit-.txt"))
   ```

## Summary

✅ **Structured Logging**: All logs use structured properties  
✅ **Rich Console**: Easy-to-read local development logs  
✅ **Application Insights**: Production logs queryable in KQL  
✅ **HTTP Logging**: All requests logged with context  
✅ **Graceful Shutdown**: Proper cleanup and flush  
✅ **Consistent Format**: Same structure local and production  

**Status**: Phase 4 complete! The application now has enterprise-grade structured logging with Serilog, enhancing both developer experience and production observability.

---

**Last Updated**: 2025-12-31
