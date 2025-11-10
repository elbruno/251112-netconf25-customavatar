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
            var region = GetRegion();
            var key = GetSubscriptionKey();

            if (string.IsNullOrEmpty(region) || string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Azure Speech credentials not configured");
                return false;
            }

            // In a real scenario, you would make a test API call here
            // For now, we just validate that the configuration exists
            await Task.CompletedTask;
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
        // Priority: Environment variable (from AppHost) > Fallback
        return _configuration["AZURE_SPEECH_REGION"] 
            ?? _configuration["AzureSpeech__Region"]
            ?? _configuration["AzureSpeech:Region"]
            ?? "westus2";
    }

    public string GetSubscriptionKey()
    {
        // Extract key from ConnectionString or direct config
        var connectionString = _configuration["ConnectionStrings:speech"];
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            // Parse Aspire connection string: "Endpoint=...;Key=...;"
            var keyMatch = System.Text.RegularExpressions.Regex.Match(
                connectionString, 
                @"Key=([^;]+)"
            );
            if (keyMatch.Success)
            {
                return keyMatch.Groups[1].Value;
            }
        }

        // Fallback to environment variables
        return _configuration["AZURE_SPEECH_API_KEY"] 
            ?? _configuration["AzureSpeech__ApiKey"]
            ?? _configuration["AzureSpeech:ApiKey"]
            ?? string.Empty;
    }
}
