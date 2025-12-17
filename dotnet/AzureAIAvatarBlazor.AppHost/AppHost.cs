var builder = DistributedApplication.CreateBuilder(args);

// ==============================
// Application Project
// ==============================

var avatarApp = builder.AddProject<Projects.AzureAIAvatarBlazor>("azureaiavatarblazor")
    .WithEnvironment("Avatar__Character", builder.Configuration["Avatar:Character"] ?? "Bruno-Avatar-03")
    .WithEnvironment("Avatar__Style", builder.Configuration["Avatar:Style"] ?? "")
    .WithEnvironment("Avatar__IsCustomAvatar", builder.Configuration["Avatar:IsCustomAvatar"] ?? "true")
    .WithEnvironment("Avatar__UseBuiltInVoice", builder.Configuration["Avatar:UseBuiltInVoice"] ?? "true")
    .WithEnvironment("OpenAI__DeploymentName", builder.Configuration["OpenAI:DeploymentName"] ?? "gpt-5.1-chat")
    .WithEnvironment("SystemPrompt", builder.Configuration["SystemPrompt"] ?? "You are Bruno Capuano. Respond in the user's language with a short answer and a friendly, approachable tone. Convert numeric times (e.g., 08:00) to spoken format (e.g., \"eight in the morning\"). If you don't know an answer, just say \"I don't know\".");

// ==============================
// Build and Run
// ==============================

builder.Build().Run();
