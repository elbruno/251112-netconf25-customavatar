var builder = DistributedApplication.CreateBuilder(args);

// ==============================
// Azure OpenAI
// ==============================

IResourceBuilder<IResourceWithConnectionString>? microsoftfoundrycnnstring = builder.AddConnectionString("microsoftfoundrycnnstring");


// ==============================
// Application Project
// ==============================

var avatarApp = builder.AddProject<Projects.AzureAIAvatarBlazor>("azureaiavatarblazor")
    .WithReference(microsoftfoundrycnnstring);

// ==============================
// Build and Run
// ==============================

builder.Build().Run();
