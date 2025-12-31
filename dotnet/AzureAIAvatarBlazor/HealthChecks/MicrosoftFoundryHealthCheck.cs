using Microsoft.Extensions.Diagnostics.HealthChecks;
using AzureAIAvatarBlazor.MAFFoundry;

namespace AzureAIAvatarBlazor.HealthChecks;

/// <summary>
/// Health check for Microsoft Foundry connectivity.
/// Verifies that the MAFFoundryAgentProvider is registered and can be resolved.
/// </summary>
public class MicrosoftFoundryHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MicrosoftFoundryHealthCheck> _logger;

    public MicrosoftFoundryHealthCheck(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<MicrosoftFoundryHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if Microsoft Foundry endpoint is configured
            var foundryEndpoint = _configuration.GetConnectionString("microsoftfoundryproject");
            
            if (string.IsNullOrWhiteSpace(foundryEndpoint))
            {
                _logger.LogWarning("Microsoft Foundry endpoint not configured");
                return HealthCheckResult.Degraded(
                    "Microsoft Foundry endpoint not configured (optional)",
                    data: new Dictionary<string, object>
                    {
                        { "configured", false },
                        { "endpoint", "not set" }
                    });
            }

            // Try to resolve the MAFFoundryAgentProvider
            var provider = _serviceProvider.GetService<MAFFoundryAgentProvider>();
            
            if (provider == null)
            {
                _logger.LogWarning("MAFFoundryAgentProvider not registered");
                return HealthCheckResult.Degraded(
                    "MAFFoundryAgentProvider not registered",
                    data: new Dictionary<string, object>
                    {
                        { "configured", true },
                        { "endpoint", foundryEndpoint },
                        { "provider_registered", false }
                    });
            }

            // If we get here, Microsoft Foundry is configured and provider is registered
            _logger.LogInformation("Microsoft Foundry health check passed");
            
            return HealthCheckResult.Healthy(
                "Microsoft Foundry is configured and provider is registered",
                data: new Dictionary<string, object>
                {
                    { "configured", true },
                    { "endpoint", foundryEndpoint },
                    { "provider_registered", true }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Microsoft Foundry health check failed");
            return HealthCheckResult.Unhealthy(
                "Microsoft Foundry health check failed",
                exception: ex);
        }
    }
}
