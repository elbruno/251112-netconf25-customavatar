namespace AzureAIAvatarBlazor.Models;

public class AgentLLMConfig
{
    public string DeploymentName { get; set; } = "gpt-5-mini";
    public string SystemPrompt { get; set; } = "You are an AI assistant that helps people find information.";
}
