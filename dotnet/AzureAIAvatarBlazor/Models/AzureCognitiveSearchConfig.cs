namespace AzureAIAvatarBlazor.Models;

/// <summary>
/// Azure Cognitive Search configuration
/// </summary>
public class AzureCognitiveSearchConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
