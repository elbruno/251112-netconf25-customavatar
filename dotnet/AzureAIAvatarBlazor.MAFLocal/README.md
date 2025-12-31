# AzureAIAvatarBlazor.MAFLocal

## Overview

Class library for **Agent-LLM mode** - enabling local Large Language Model (LLM) integration with Microsoft Agents Framework (MAF).

## Purpose

This library provides:

- Local LLM model integration for offline scenarios
- Agent operations with locally-hosted models
- Reduced latency for local-first architectures
- Privacy-focused agent execution without cloud dependencies

## Status

⚠️ **In Development** - This is a placeholder project created for future local LLM integration.

## Dependencies

- `Microsoft.Agents.AI` (v1.0.0-preview.251219.1)
- `Microsoft.Agents.AI.Abstractions` (v1.0.0-preview.251219.1)
- `Microsoft.Agents.AI.Hosting` (v1.0.0-preview.251219.1)
- `Azure.AI.OpenAI` (v2.8.0-beta.1) - For API compatibility
- `Azure.Identity` (v1.18.0-beta.2)

## Use Cases

- **Offline scenarios**: Run agents without internet connectivity
- **Privacy-sensitive workloads**: Keep data processing local
- **Low-latency requirements**: Eliminate round-trip to cloud services
- **Development/testing**: Test agent logic without cloud costs

## Usage

```csharp
// TODO: Add usage examples once implementation is complete
```

## Implementation Roadmap

- [ ] Define local LLM service interfaces
- [ ] Implement local model loading and inference
- [ ] Add MAF agent integration for local execution
- [ ] Create configuration system for model selection
- [ ] Add streaming response support
- [ ] Implement fallback to cloud when needed
- [ ] Add unit tests
- [ ] Document supported local LLM providers (Ollama, LM Studio, etc.)

## Supported Local LLM Providers (Planned)

- Ollama
- LM Studio
- llama.cpp
- ONNX Runtime
- Custom OpenAI-compatible endpoints

## Related Projects

- **AzureAIAvatarBlazor.MAFFoundry** - Cloud-based agent capabilities
- **AzureAIAvatarBlazor** - Main Blazor application
