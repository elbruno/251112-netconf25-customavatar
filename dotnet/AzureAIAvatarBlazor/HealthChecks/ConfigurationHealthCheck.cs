using Microsoft.Extensions.Diagnostics.HealthChecks;
using AzureAIAvatarBlazor.Services;

namespace AzureAIAvatarBlazor.HealthChecks;

/// <summary>
/// Health check for application configuration validation.
/// Verifies that all required configuration values are present and valid.
/// </summary>
public class ConfigurationHealthCheck : IHealthCheck
{
    private readonly ConfigurationService _configService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationHealthCheck> _logger;

    public ConfigurationHealthCheck(
        ConfigurationService configService,
        IConfiguration configuration,
        ILogger<ConfigurationHealthCheck> logger)
    {
        _configService = configService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _configService.GetConfiguration();
            var issues = new List<string>();
            var data = new Dictionary<string, object>();

            // Check Speech Service configuration
            if (string.IsNullOrWhiteSpace(config.AzureSpeech.Region))
            {
                issues.Add("Azure Speech Service region not configured");
                data["speech_region"] = "missing";
            }
            else
            {
                data["speech_region"] = config.AzureSpeech.Region;
            }

            if (string.IsNullOrWhiteSpace(config.AzureSpeech.ApiKey))
            {
                issues.Add("Azure Speech Service API key not configured");
                data["speech_api_key"] = "missing";
            }
            else
            {
                data["speech_api_key"] = "configured";
            }

            // Check Avatar configuration
            if (string.IsNullOrWhiteSpace(config.Avatar.Character))
            {
                issues.Add("Avatar character not configured");
                data["avatar_character"] = "missing";
            }
            else
            {
                data["avatar_character"] = config.Avatar.Character;
            }

            // Check OpenAI/Foundry configuration
            var foundryEndpoint = _configuration.GetConnectionString("microsoftfoundryproject");
            if (string.IsNullOrWhiteSpace(foundryEndpoint))
            {
                issues.Add("Microsoft Foundry endpoint not configured (optional)");
                data["foundry_endpoint"] = "not configured";
            }
            else
            {
                data["foundry_endpoint"] = "configured";
            }

            // Check Application Insights configuration (optional)
            var appInsightsConnectionString = _configuration.GetConnectionString("appinsights");
            data["app_insights"] = string.IsNullOrWhiteSpace(appInsightsConnectionString) 
                ? "not configured (optional)" 
                : "configured";

            // Determine health status
            if (issues.Count == 0)
            {
                _logger.LogInformation("Configuration health check passed");
                return HealthCheckResult.Healthy(
                    "All required configuration values are present",
                    data: data);
            }
            else if (issues.Any(i => !i.Contains("optional")))
            {
                var criticalIssues = issues.Where(i => !i.Contains("optional")).ToList();
                _logger.LogWarning("Configuration has {Count} critical issues", criticalIssues.Count);
                return HealthCheckResult.Degraded(
                    $"Configuration issues: {string.Join(", ", criticalIssues)}",
                    data: data);
            }
            else
            {
                _logger.LogInformation("Configuration health check passed with optional warnings");
                return HealthCheckResult.Healthy(
                    "Required configuration present, optional items missing",
                    data: data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration health check failed");
            return HealthCheckResult.Unhealthy(
                "Configuration health check failed",
                exception: ex);
        }
    }
}
