namespace AzureAIAvatarBlazor.Models;

/// <summary>
/// Represents a prompt profile configuration
/// </summary>
public class PromptProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Defaults { get; set; } = new();
}

/// <summary>
/// Container for prompt profiles
/// </summary>
public class PromptProfilesContainer
{
    public List<PromptProfile> Profiles { get; set; } = new();
}
