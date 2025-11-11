namespace AzureAIAvatarBlazor.Services;

/// <summary>
/// Service for interacting with Azure Speech Service (STT/TTS/Avatar)
/// Note: Most of the Speech SDK functionality for avatar needs to run in the browser
/// using JavaScript interop due to WebRTC and media stream requirements
/// </summary>
public interface IAzureSpeechService
{
    Task<bool> ValidateConnectionAsync();
    string GetRegion();
    string GetSubscriptionKey();
}

public class AzureSpeechService : IAzureSpeechService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureSpeechService> _logger;

    public AzureSpeechService(IConfiguration configuration, ILogger<AzureSpeechService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> ValidateConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Validating Azure Speech connection...");

            var region = GetRegion();
            var key = GetSubscriptionKey();

            if (string.IsNullOrEmpty(region) || string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Azure Speech credentials not configured - Region: {HasRegion}, Key: {HasKey}",
                    !string.IsNullOrEmpty(region),
                    !string.IsNullOrEmpty(key));
                return false;
            }

            _logger.LogInformation("Azure Speech credentials configured - Region: {Region}", region);

            // In a real scenario, you would make a test API call here
            // For now, we just validate that the configuration exists
            await Task.CompletedTask;

            _logger.LogInformation("Azure Speech connection validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Azure Speech connection");
            return false;
        }
    }

    public string GetRegion()
    {
        // Try to extract region from connection string endpoint first
        var connectionString = _configuration["ConnectionStrings:speech"];
        if (!string.IsNullOrEmpty(connectionString))
        {
            // Parse endpoint like "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=..."
            var endpointMatch = System.Text.RegularExpressions.Regex.Match(
                connectionString,
                @"Endpoint=https://([^.]+)\.(?:api\.cognitive\.microsoft\.com|tts\.speech\.microsoft\.com)"
            );
            if (endpointMatch.Success)
            {
                var region = endpointMatch.Groups[1].Value;
                _logger.LogInformation("Extracted region from connection string endpoint: {Region}", region);
                return region;
            }
        }

        // Fallback to explicit configuration
        var configRegion = _configuration["AZURE_SPEECH_REGION"]
            ?? _configuration["AzureSpeech__Region"]
            ?? _configuration["AzureSpeech:Region"];

        if (!string.IsNullOrEmpty(configRegion))
        {
            _logger.LogInformation("Using configured region: {Region}", configRegion);
            return configRegion;
        }

        // Default fallback
        _logger.LogWarning("No region configured, using default: westus2");
        return "westus2";
    }

    public string GetSubscriptionKey()
    {
        _logger.LogDebug("Retrieving Azure Speech subscription key...");

        // Extract key from ConnectionString or direct config
        var connectionString = _configuration["ConnectionStrings:speech"];

        if (!string.IsNullOrEmpty(connectionString))
        {
            _logger.LogDebug("Found connection string for speech service");

            // Parse Aspire connection string: "Endpoint=...;Key=...;"
            var keyMatch = System.Text.RegularExpressions.Regex.Match(
                connectionString,
                @"Key=([^;]+)"
            );
            if (keyMatch.Success)
            {
                _logger.LogInformation("Extracted subscription key from connection string");
                return keyMatch.Groups[1].Value;
            }
        }

        // Fallback to environment variables
        var key = _configuration["AZURE_SPEECH_API_KEY"]
            ?? _configuration["AzureSpeech__ApiKey"]
            ?? _configuration["AzureSpeech:ApiKey"]
            ?? string.Empty;

        if (!string.IsNullOrEmpty(key))
        {
            _logger.LogInformation("Retrieved subscription key from environment configuration");
        }
        else
        {
            _logger.LogWarning("Azure Speech subscription key not found in configuration");
        }

        return key;
    }
}
