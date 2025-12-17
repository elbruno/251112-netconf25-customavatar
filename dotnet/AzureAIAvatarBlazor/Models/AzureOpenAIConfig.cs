namespace AzureAIAvatarBlazor.Models;

public class AzureOpenAIConfig
{
    public string Mode { get; set; } = "Agent-LLM";
    public string TenantId { get; set; } = string.Empty;

    public AgentLLMConfig AgentLLM { get; set; } = new();
    public AgentAIFoundryConfig AgentAIFoundry { get; set; } = new();
    public AgentMicrosoftFoundryConfig AgentMicrosoftFoundry { get; set; } = new();
}