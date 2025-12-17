namespace AzureAIAvatarBlazor.Models;

/// <summary>
/// Azure OpenAI configuration organized by mode sections
/// </summary>
public class AzureOpenAIConfig
{
    public string Mode { get; set; } = "Agent-LLM";

    public AgentLLMConfig AgentLLM { get; set; } = new();
    public AgentAIFoundryConfig AgentAIFoundry { get; set; } = new();
    public AgentMicrosoftFoundryConfig AgentMicrosoftFoundry { get; set; } = new();

    public string TenantId { get; set; } = string.Empty;
}