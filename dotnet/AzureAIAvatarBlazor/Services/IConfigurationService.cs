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
}
