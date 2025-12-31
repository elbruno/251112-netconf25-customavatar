using Microsoft.Extensions.Diagnostics.HealthChecks;
using AzureAIAvatarBlazor.Services;

namespace AzureAIAvatarBlazor.HealthChecks;

/// <summary>
/// Health check for Azure Speech Service connectivity.
/// Verifies that the Speech Service configuration is valid.
/// </summary>
public class AzureSpeechHealthCheck : IHealthCheck
{
    private readonly ConfigurationService _configService;
    private readonly ILogger<AzureSpeechHealthCheck> _logger;

    public AzureSpeechHealthCheck(
        ConfigurationService configService,
        ILogger<AzureSpeechHealthCheck> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _configService.GetConfiguration();
            var speechConfig = config.AzureSpeech;

            // Check if Speech Service is configured
            if (string.IsNullOrWhiteSpace(speechConfig.Region))
            {
                _logger.LogWarning("Azure Speech Service region not configured");
                return HealthCheckResult.Degraded(
                    "Azure Speech Service region not configured",
                    data: new Dictionary<string, object>
                    {
                        { "region_configured", false },
                        { "api_key_configured", !string.IsNullOrWhiteSpace(speechConfig.ApiKey) }
                    });
            }

            if (string.IsNullOrWhiteSpace(speechConfig.ApiKey))
            {
                _logger.LogWarning("Azure Speech Service API key not configured");
                return HealthCheckResult.Degraded(
                    "Azure Speech Service API key not configured",
                    data: new Dictionary<string, object>
                    {
                        { "region_configured", true },
                        { "region", speechConfig.Region },
                        { "api_key_configured", false }
                    });
            }

            // Configuration looks valid
            _logger.LogInformation("Azure Speech Service configuration is valid");
            
            return HealthCheckResult.Healthy(
                "Azure Speech Service is configured",
                data: new Dictionary<string, object>
                {
                    { "region_configured", true },
                    { "region", speechConfig.Region },
                    { "api_key_configured", true },
                    { "private_endpoint_enabled", speechConfig.EnablePrivateEndpoint }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Speech Service health check failed");
            return HealthCheckResult.Unhealthy(
                "Azure Speech Service health check failed",
                exception: ex);
        }
    }
}
