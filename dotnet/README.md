# Azure AI Avatar - .NET 10 Blazor

This repository demonstrates Azure Avatars using .NET 10 and Blazor with enterprise-ready patterns including Microsoft Foundry integration and Application Insights telemetry.

## Key Features

- **Microsoft Foundry Integration**: Uses Azure AI Foundry for agent management via the `AzureAIAvatarBlazor.MAFFoundry` library
- **Aspire Orchestration**: All credentials and endpoints managed by .NET Aspire AppHost
- **Application Insights**: Comprehensive telemetry and monitoring (Phase 1)
- **Health Checks**: Production-ready health endpoints for monitoring (Phase 2)
- **Interactive Avatars**: Real-time talking avatars with Azure Speech Service
- **Simplified Configuration**: AppHost-managed secrets (no manual configuration needed for Microsoft Foundry endpoint or Application Insights)

## Architecture

The application consists of:
- **AzureAIAvatarBlazor**: Main Blazor Server application
- **AzureAIAvatarBlazor.AppHost**: Aspire orchestration layer
- **AzureAIAvatarBlazor.ServiceDefaults**: Shared telemetry and resilience configuration
- **AzureAIAvatarBlazor.MAFFoundry**: Microsoft Foundry integration library (provides IChatClient)

## Configuration Management

### AppHost-Managed (Connection Strings)
These are managed by Aspire AppHost and **NOT** editable in the UI:
- **microsoftfoundryproject**: Microsoft Foundry project endpoint
- **tenantId**: Azure tenant ID for authentication
- **appinsights**: Application Insights connection string

### User-Configurable (Config UI)
These can be changed in the Configuration page:
- System Prompt / Instructions
- Deployment Name (AI model)
- Agent Name (Microsoft Foundry)
- Avatar character and style
- Speech settings (voice, locales)
- Audio gain

## Quick Start

See [docs/QUICKSTART.md](docs/QUICKSTART.md) for detailed setup instructions.

```bash
# 1. Install prerequisites
dotnet workload install aspire

# 2. Navigate to AppHost
cd dotnet/AzureAIAvatarBlazor.AppHost

# 3. Configure connection strings (user secrets)
dotnet user-secrets set "ConnectionStrings:microsoftfoundryproject" "https://YOUR_PROJECT.services.ai.azure.com/api/projects/YOUR_PROJECT_ID"
dotnet user-secrets set "ConnectionStrings:tenantId" "YOUR_TENANT_ID"
dotnet user-secrets set "ConnectionStrings:appinsights" "InstrumentationKey=...;IngestionEndpoint=..."

# 4. Run the application
dotnet run
```

## Documentation

- **[QUICKSTART.md](docs/QUICKSTART.md)**: Getting started guide
- **[ARCHITECTURE.md](docs/ARCHITECTURE.md)**: Technical architecture details
- **[MAFFOUNDRY_LIBRARY.md](docs/MAFFOUNDRY_LIBRARY.md)**: Microsoft Foundry integration guide
- **[ASPIRE_ENHANCEMENT_PLAN.md](docs/ASPIRE_ENHANCEMENT_PLAN.md)**: Enhancement roadmap (Phases 1-8)
- **[PHASE1_IMPLEMENTATION_SUMMARY.md](docs/PHASE1_IMPLEMENTATION_SUMMARY.md)**: Application Insights implementation
- **[PHASE2_IMPLEMENTATION_SUMMARY.md](docs/PHASE2_IMPLEMENTATION_SUMMARY.md)**: Health Checks implementation

## Aspire Enhancement Phases

- ✅ **Phase 1: Application Insights** - Custom telemetry and monitoring (COMPLETE)
- ✅ **Phase 2: Health Checks** - Readiness and liveness probes (COMPLETE)
- 📋 **Phase 3: Redis Caching** - Configuration and session state caching
- 📋 **Phase 4: Structured Logging** - Enhanced logging with Serilog
- 📋 **Phase 5: Distributed Tracing** - Custom spans for avatar operations
- 📋 **Phase 6: Container Registry** - ACR integration for deployments
- 📋 **Phase 7: Managed Identity** - Eliminate secrets in production
- 📋 **Phase 8: Multi-Environment** - Dev/Staging/Prod configurations

## Key Changes

This implementation uses Microsoft Foundry for AI operations instead of direct Azure OpenAI:
- **Before**: Direct Azure OpenAI configuration with endpoint/API key in UI
- **After**: Microsoft Foundry endpoint managed by Aspire AppHost, IChatClient automatically registered

The Configuration UI now focuses on user-customizable settings only. Infrastructure secrets are managed securely through Aspire connection strings.

