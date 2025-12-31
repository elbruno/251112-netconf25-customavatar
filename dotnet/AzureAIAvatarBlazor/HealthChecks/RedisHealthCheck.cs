using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace AzureAIAvatarBlazor.HealthChecks;

/// <summary>
/// Health check for Redis connectivity.
/// Verifies that Redis is accessible and responding to commands.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(
        IConnectionMultiplexer redis,
        ILogger<RedisHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if Redis is connected
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("Redis is not connected");
                return HealthCheckResult.Unhealthy(
                    "Redis is not connected",
                    data: new Dictionary<string, object>
                    {
                        { "connected", false }
                    });
            }

            // Try to ping Redis
            var db = _redis.GetDatabase();
            var pingResult = await db.PingAsync();

            _logger.LogInformation("Redis health check passed with ping: {Ping}ms", pingResult.TotalMilliseconds);

            return HealthCheckResult.Healthy(
                $"Redis is healthy (ping: {pingResult.TotalMilliseconds:F2}ms)",
                data: new Dictionary<string, object>
                {
                    { "connected", true },
                    { "ping_ms", pingResult.TotalMilliseconds }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy(
                "Redis health check failed",
                exception: ex);
        }
    }
}
