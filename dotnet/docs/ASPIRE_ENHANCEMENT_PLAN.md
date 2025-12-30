# Aspire Enhancement Plan

This document outlines a phased approach to enhance the .NET Aspire implementation of the Azure AI Avatar Agent application with additional cloud-native features that work both locally and in production.

## Overview

The enhancement plan focuses on adding enterprise-grade observability, resilience, and cloud-native capabilities to the Aspire orchestration layer. Each phase is designed to be independently deployable and testable, with a focus on features that enhance the development and production experience.

## Phase 1: Application Insights Integration ⭐ **START HERE**

### Objective
Integrate Azure Application Insights for comprehensive application monitoring, custom telemetry, and production diagnostics.

### Benefits
- **Local Development**: View telemetry in Aspire Dashboard
- **Production**: Full monitoring with Application Insights in Azure Portal
- **Works Everywhere**: Compatible with local dev, Azure Container Apps, and App Service

### Implementation Tasks

1. **Add Application Insights Aspire Component**
   - Add `Aspire.Azure.ApplicationInsights` NuGet package to AppHost
   - Configure Application Insights connection string in AppHost.cs
   - Wire up the Application Insights resource to the Blazor app

2. **Configure Application Insights in Blazor App**
   - Add Application Insights SDK packages
   - Register Application Insights services in Program.cs
   - Configure telemetry collection for:
     - HTTP requests and responses
     - Dependencies (Azure OpenAI, Speech Service)
     - Custom events (avatar sessions, chat interactions)
     - Exceptions and errors

3. **Add Custom Telemetry**
   - Track avatar session lifecycle (start, end, duration)
   - Monitor AI agent response times
   - Track speech synthesis operations
   - Log configuration changes
   - Monitor WebRTC connection health

4. **Update Documentation**
   - Add Application Insights setup to QUICKSTART.md
   - Document custom telemetry events in ARCHITECTURE.md
   - Add troubleshooting section for viewing telemetry

### Success Criteria
- ✅ Application Insights visible in Aspire Dashboard during local development
- ✅ Custom telemetry events captured for avatar operations
- ✅ Dependencies tracked for Azure services
- ✅ Exception tracking enabled
- ✅ Documentation updated with setup instructions

### Files to Modify
- `dotnet/AzureAIAvatarBlazor.AppHost/AzureAIAvatarBlazor.AppHost.csproj` (add NuGet)
- `dotnet/AzureAIAvatarBlazor.AppHost/AppHost.cs` (configure AppInsights)
- `dotnet/AzureAIAvatarBlazor/AzureAIAvatarBlazor.csproj` (add SDK)
- `dotnet/AzureAIAvatarBlazor/Program.cs` (register services)
- `dotnet/AzureAIAvatarBlazor/Services/AzureAIAgentService.cs` (add telemetry)
- `dotnet/AzureAIAvatarBlazor/Services/ConfigurationService.cs` (add telemetry)
- `dotnet/docs/QUICKSTART.md` (update instructions)
- `dotnet/docs/ARCHITECTURE.md` (document telemetry)

---

## Phase 2: Health Checks and Readiness Probes

### Objective
Implement comprehensive health checks for all Azure service dependencies and expose them via Aspire Health Checks dashboard.

### Benefits
- **Local Development**: Immediately see which services are healthy
- **Production**: Kubernetes/Container Apps can auto-restart unhealthy instances
- **Works Everywhere**: Health check endpoints work in any hosting environment

### Implementation Tasks

1. **Add Health Check Packages**
   - Add `Aspire.HealthChecks` component
   - Add health check packages for HTTP, Azure services

2. **Implement Service Health Checks**
   - Azure OpenAI connectivity check
   - Azure Speech Service connectivity check
   - Configuration validation check
   - Memory/CPU resource checks

3. **Configure Health Check Endpoints**
   - `/health` - Basic liveness check
   - `/health/ready` - Readiness check (all dependencies)
   - `/health/startup` - Startup check

4. **Integrate with Aspire Dashboard**
   - Display health status in dashboard
   - Configure health check intervals

### Files to Modify
- AppHost configuration
- Program.cs (health check registration)
- New file: `HealthChecks/AzureOpenAIHealthCheck.cs`
- New file: `HealthChecks/AzureSpeechHealthCheck.cs`

---

## Phase 3: Redis Caching for Configuration and Sessions

### Objective
Add Redis caching layer for configuration data, conversation history, and avatar session state.

### Benefits
- **Local Development**: Aspire automatically provisions Redis container
- **Production**: Azure Cache for Redis integration
- **Works Everywhere**: Consistent caching behavior across environments

### Implementation Tasks

1. **Add Redis Aspire Component**
   - Add Redis container to AppHost for local dev
   - Configure Azure Cache for Redis for production

2. **Implement Caching Service**
   - Cache avatar configuration
   - Cache conversation history (optional, for multi-instance scenarios)
   - Cache avatar token responses

3. **Update Services**
   - ConfigurationService: Add caching layer
   - AzureAIAgentService: Cache agent instances

### Files to Modify
- AppHost.cs (add Redis)
- New file: `Services/CachingService.cs`
- ConfigurationService.cs (add caching)
- Program.cs (register Redis)

---

## Phase 4: Structured Logging with Serilog

### Objective
Replace default logging with Serilog for structured logging, enrichment, and better log aggregation.

### Benefits
- **Local Development**: Rich console logs with colors and structure
- **Production**: Structured logs easily queryable in Application Insights
- **Works Everywhere**: Consistent log format across all environments

### Implementation Tasks

1. **Add Serilog Packages**
   - Add Serilog and Application Insights sink
   - Configure structured logging

2. **Configure Log Enrichers**
   - Add correlation IDs
   - Add user context
   - Add environment info

3. **Update Logging Calls**
   - Use structured logging throughout services
   - Add relevant context properties

### Files to Modify
- Program.cs (configure Serilog)
- All service files (update logging calls)

---

## Phase 5: Distributed Tracing Enhancements

### Objective
Enhance OpenTelemetry tracing with custom spans for avatar operations, AI calls, and speech synthesis.

### Benefits
- **Local Development**: View detailed traces in Aspire Dashboard
- **Production**: End-to-end tracing in Application Insights
- **Works Everywhere**: Standard OpenTelemetry format

### Implementation Tasks

1. **Add Custom Spans**
   - Avatar session lifecycle spans
   - AI agent call spans with token counts
   - Speech synthesis spans

2. **Add Span Attributes**
   - Avatar character and style
   - Model deployment name
   - Response times and token counts

3. **Configure Trace Sampling**
   - 100% sampling in development
   - Adaptive sampling in production

### Files to Modify
- ServiceDefaults (configure tracing)
- All service files (add custom spans)

---

## Phase 6: Azure Container Registry Integration

### Objective
Configure Aspire to publish container images to Azure Container Registry (ACR) for production deployments.

### Benefits
- **Production Only**: Enables deployment to Azure Container Apps
- **CI/CD**: Automated container builds
- **Works in Azure**: Required for production container deployments

### Implementation Tasks

1. **Configure ACR in AppHost**
   - Add ACR connection
   - Configure image publishing

2. **Update Deployment Scripts**
   - Add container build tasks
   - Configure azd integration

### Files to Modify
- AppHost.cs (add ACR)
- New file: `azure.yaml` (azd configuration)

---

## Phase 7: Service-to-Service Authentication with Managed Identity

### Objective
Replace API keys with managed identity authentication for Azure services in production.

### Benefits
- **Production Only**: Eliminates secrets in production
- **Security**: Uses Azure AD authentication
- **Local Dev**: Falls back to API keys

### Implementation Tasks

1. **Configure Managed Identity**
   - Update AppHost to provision managed identity
   - Configure Azure RBAC roles

2. **Update Service Clients**
   - Use DefaultAzureCredential
   - Fallback to API keys in development

3. **Update Configuration**
   - Make API keys optional in production
   - Document managed identity setup

### Files to Modify
- AppHost.cs (managed identity)
- ConfigurationService.cs (credential handling)
- AzureAIAgentService.cs (credential handling)
- Documentation files

---

## Phase 8: Multi-Environment Configuration

### Objective
Support multiple environment configurations (Development, Staging, Production) with environment-specific settings.

### Benefits
- **Local Development**: Use local/dev resources
- **Staging**: Use staging resources
- **Production**: Use production resources
- **Works Everywhere**: Standard .NET configuration patterns

### Implementation Tasks

1. **Add Environment-Specific Config Files**
   - appsettings.Development.json
   - appsettings.Staging.json
   - appsettings.Production.json

2. **Configure Environment Detection**
   - Update AppHost to detect environment
   - Load appropriate configuration

3. **Document Configuration Strategy**
   - Local dev: User secrets
   - Staging/Prod: Environment variables or Key Vault

### Files to Modify
- New files: Environment-specific appsettings
- Documentation updates

---

## Implementation Priority

Based on local compatibility and value:

### High Priority (Local + Production)
1. ✅ **Phase 1: Application Insights** - Works locally via Aspire Dashboard
2. ✅ **Phase 2: Health Checks** - Essential for production readiness
3. ✅ **Phase 3: Redis Caching** - Aspire provides local Redis container
4. ✅ **Phase 4: Structured Logging** - Better local and production logs
5. ✅ **Phase 5: Distributed Tracing** - Enhanced visibility everywhere

### Medium Priority (Production-Focused)
6. **Phase 6: Azure Container Registry** - Production deployment only
7. **Phase 7: Managed Identity** - Production security

### Low Priority (Nice to Have)
8. **Phase 8: Multi-Environment Config** - Standardization

---

## Getting Started with Phase 1

To begin implementing Phase 1 (Application Insights Integration):

1. Review the Phase 1 tasks above
2. Install required NuGet packages
3. Configure Application Insights in AppHost
4. Add custom telemetry to services
5. Test locally with Aspire Dashboard
6. Update documentation

See implementation details in the Phase 1 section above.

---

## Notes

- Each phase is designed to be independently deployable
- All phases are tested to work with local development via Aspire
- Production-only features gracefully degrade in local development
- Documentation is updated as part of each phase
- All enhancements follow .NET Aspire best practices

---

Last Updated: 2025-12-30
