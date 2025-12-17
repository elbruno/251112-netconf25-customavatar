using AzureAIAvatarBlazor.Models;

namespace AzureAIAvatarBlazor.Services;

/// <summary>
/// Service for managing application configuration
/// </summary>
public interface IConfigurationService
{
    AvatarConfiguration GetConfiguration();
    Task SaveConfigurationAsync(AvatarConfiguration config);
    Task<string?> ValidateConfigurationAsync(AvatarConfiguration config);
    Task<List<PromptProfile>> GetPromptProfilesAsync();
    Task<string> GetPromptProfileContentAsync(string fileName);

    /// <summary>
    /// Event raised when the configuration is saved or updated in memory
    /// Subscribers should update their local view/state when this event fires.
    /// </summary>
    event EventHandler<AvatarConfiguration?>? ConfigurationChanged;
}
