namespace AzureAIAvatarBlazor.Models;

public class AgentLLMConfig
{
    public string DeploymentName { get; set; } = "gpt-5.1-chat";
    public string SystemPrompt { get; set; } = "You are an AI assistant that helps people find information.";
}
