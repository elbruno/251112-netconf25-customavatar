var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AzureAIAvatarBlazor>("azureaiavatarblazor");

builder.Build().Run();
