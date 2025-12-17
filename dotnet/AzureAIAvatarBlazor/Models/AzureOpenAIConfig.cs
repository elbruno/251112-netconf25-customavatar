namespace AzureAIAvatarBlazor.Models;

public class AzureOpenAI
{
    public string Mode { get; set; } = "Agent-LLM";
    public string TenantId { get; set; } = string.Empty;

    public AgentLLMConfig AgentLLM { get; set; } = new();
    public AgentAIFoundryConfig AgentAIFoundry { get; set; } = new();
    public AgentMicrosoftFoundryConfig AgentMicrosoftFoundry { get; set; } = new();
}