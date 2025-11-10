# Azure AI Avatar Demo - .NET 9 Blazor Implementation

A modern, server-side Blazor application that demonstrates Azure AI capabilities including Speech Services (STT/TTS with Talking Avatar) and Azure OpenAI for intelligent conversations.

## 🚀 Features

### Core Capabilities
- **Interactive Chat**: Real-time conversations with AI-powered avatars
- **Speech-to-Text (STT)**: Multi-language voice input recognition
- **Text-to-Speech (TTS)**: Natural-sounding voice synthesis
- **Talking Avatar**: WebRTC-based video streaming with synchronized lip movement
- **Azure OpenAI Integration**: Streaming chat completions for natural conversations
- **Custom Avatars**: Support for custom avatar characters and styles
- **Multi-language Support**: Automatic language detection for multiple locales

### Technical Features
- **Built with .NET 9**: Latest .NET framework with improved performance
- **Blazor Server**: Real-time interactivity with SignalR
- **Azure AI SDKs**: 
  - `Azure.AI.OpenAI` (2.1.0) for chat completions
  - `Microsoft.CognitiveServices.Speech` (1.41.1) for speech and avatar
  - `Azure.Search.Documents` (11.7.0) for On Your Data scenarios
- **Modern UI**: Bootstrap 5 with Bootstrap Icons
- **JavaScript Interop**: Seamless integration with Azure Speech SDK
- **Secure Configuration**: User secrets and appsettings.json

## 📋 Prerequisites

Before you begin, ensure you have the following:

### Required Software
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- A modern web browser (Chrome, Edge, Firefox, Safari)
- Visual Studio 2022, Visual Studio Code, or Rider (recommended)

### Required Azure Resources
1. **Azure Speech Service**
   - Create a Speech resource in Azure Portal
   - Note the API Key and Region
   - Enable Avatar feature if using custom avatars

2. **Azure OpenAI Service**
   - Create an Azure OpenAI resource
   - Deploy a chat model (e.g., gpt-4o-mini, gpt-4)
   - Note the Endpoint, API Key, and Deployment Name

3. **Azure Cognitive Search** (Optional - for "On Your Data" scenarios)
   - Create a Cognitive Search service
   - Create and populate a search index
   - Note the Endpoint, API Key, and Index Name

## 🛠️ Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/elbruno/customavatarlabs.git
cd customavatarlabs/dotnet/AzureAIAvatarBlazor
```

### 2. Configure Azure Credentials

You have two options for configuration:

#### Option A: User Secrets (Recommended for Development)

```bash
# Initialize user secrets
dotnet user-secrets init

# Set Azure Speech credentials
dotnet user-secrets set "AzureSpeech:Region" "westus2"
dotnet user-secrets set "AzureSpeech:ApiKey" "your-speech-api-key"

# Set Azure OpenAI credentials
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-openai-api-key"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"

# Optional: Set Azure Cognitive Search credentials
dotnet user-secrets set "AzureCognitiveSearch:Endpoint" "https://your-search.search.windows.net"
dotnet user-secrets set "AzureCognitiveSearch:ApiKey" "your-search-api-key"
dotnet user-secrets set "AzureCognitiveSearch:IndexName" "your-index-name"
```

#### Option B: appsettings.json (Not Recommended - Less Secure)

Edit `appsettings.json` and add your credentials:

```json
{
  "AzureSpeech": {
    "Region": "westus2",
    "ApiKey": "your-speech-api-key"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com",
    "ApiKey": "your-openai-api-key",
    "DeploymentName": "gpt-4o-mini"
  }
}
```

⚠️ **Security Warning**: Never commit API keys to source control!

### 3. Build and Run

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

The application will start and be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

## 📖 Usage Guide

### Getting Started

1. **Navigate to Home Page**: Open your browser to the application URL
2. **Configure Settings**: Click "Configure" or go to `/config`
3. **Enter Credentials**: Add your Azure service credentials
4. **Save Configuration**: Click "Save Configuration"
5. **Start Chatting**: Navigate to "Chat" page and click "Open Avatar Session"

### Configuration Options

#### Azure Speech Service
- **Region**: Select your Azure Speech resource region
- **API Key**: Enter your Speech Service subscription key
- **Private Endpoint**: Optional custom endpoint URL

#### Azure OpenAI
- **Endpoint**: Your Azure OpenAI resource endpoint
- **API Key**: Your Azure OpenAI subscription key
- **Deployment Name**: The name of your deployed model
- **System Prompt**: Customize the AI assistant's behavior

#### STT/TTS Settings
- **STT Locales**: Comma-separated list of language codes (e.g., `en-US,es-ES,fr-FR`)
- **TTS Voice**: Select from available neural voices
- **Custom Voice**: Optional endpoint ID for custom voices
- **Continuous Conversation**: Enable for ongoing dialogue

#### Avatar Settings
- **Character**: Select avatar character (Lisa, Harry, Jeff, etc.)
- **Style**: Choose avatar style (casual-sitting, business, formal)
- **Custom Avatar**: Enable for custom avatar support
- **Use Built-In Voice**: Use avatar's native voice
- **Enable Subtitles**: Show text subtitles during speech
- **Audio Gain**: Adjust volume amplification (0.1x - 5.0x)

### Using the Chat Interface

1. **Open Avatar Session**: Click the button to establish WebRTC connection
2. **Type Messages**: Enable "Type Message" checkbox and send text
3. **View Conversation**: Messages appear in the conversation panel
4. **Stop Speaking**: Interrupt the avatar at any time
5. **Clear History**: Reset conversation while keeping system prompt
6. **Close Session**: Disconnect when finished

## 🏗️ Architecture Overview

### Technology Stack

```
┌─────────────────────────────────────────┐
│         Blazor Server (.NET 9)          │
├─────────────────────────────────────────┤
│  Components  │  Services  │   Models    │
│  - Chat      │  - OpenAI  │  - Config   │
│  - Config    │  - Speech  │  - Messages │
│  - Home      │  - Config  │  - Profiles │
├─────────────────────────────────────────┤
│        JavaScript Interop Layer         │
│     (avatar-interop.js + Speech SDK)    │
├─────────────────────────────────────────┤
│            Azure Services               │
│  ┌──────────┬──────────┬──────────┐   │
│  │  Speech  │  OpenAI  │  Search  │   │
│  │  Service │  Service │  Service │   │
│  └──────────┴──────────┴──────────┘   │
└─────────────────────────────────────────┘
```

### Component Architecture

- **Pages**: Blazor components for UI (`Chat.razor`, `Config.razor`, `Home.razor`)
- **Services**: 
  - `AzureOpenAIService`: Handles chat completions with streaming
  - `AzureSpeechService`: Provides speech credentials
  - `ConfigurationService`: Manages application settings
- **Models**: Data structures for configuration and messages
- **JavaScript**: Browser-side WebRTC and Speech SDK integration

### Data Flow

1. User interacts with Blazor UI
2. UI calls C# service methods
3. Services communicate with Azure APIs
4. JavaScript interop handles WebRTC/Speech SDK
5. Real-time updates via SignalR

## 🔒 Security Best Practices

### Credential Management
- ✅ Use **User Secrets** for local development
- ✅ Use **Azure Key Vault** for production
- ✅ Use **Managed Identities** when possible
- ❌ Never commit secrets to source control
- ❌ Don't hardcode API keys in code

### Configuration Files
- `.gitignore` excludes sensitive files
- `appsettings.json` should only contain non-sensitive defaults
- `appsettings.Development.json` is for development overrides
- User secrets stored outside project directory

## 🐛 Troubleshooting

### Common Issues

#### Avatar Session Won't Start
- **Check credentials**: Verify Speech API key and region in configuration
- **Check browser**: Ensure WebRTC is supported (Chrome, Edge, Firefox, Safari)
- **Check network**: WebRTC requires network access for STUN/TURN servers
- **Check console**: Open browser DevTools for JavaScript errors

#### No Audio from Avatar
- **Check volume**: Adjust Audio Gain in configuration
- **Check permissions**: Allow microphone/audio in browser
- **Check audio device**: Ensure speakers are connected and working
- **Check Web Audio**: Some browsers may block autoplay

#### Chat Not Responding
- **Check OpenAI credentials**: Verify endpoint, key, and deployment name
- **Check quota**: Ensure you haven't exceeded API rate limits
- **Check deployment**: Verify model is deployed and accessible
- **Check network**: Ensure connectivity to Azure OpenAI

#### Build Errors
- **Restore packages**: Run `dotnet restore`
- **Check .NET version**: Ensure .NET 9 SDK is installed
- **Clean build**: Run `dotnet clean` then `dotnet build`
- **Check NuGet**: Clear NuGet cache if packages fail to restore

## 📚 Additional Resources

### Documentation
- [Azure Speech Service Documentation](https://docs.microsoft.com/azure/cognitive-services/speech-service/)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)
- [.NET 9 Documentation](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9)

### Samples and Tutorials
- [Azure Speech SDK Samples](https://github.com/Azure-Samples/cognitive-services-speech-sdk)
- [Azure OpenAI Samples](https://github.com/Azure-Samples/openai-dotnet-samples)
- [Blazor Samples](https://github.com/dotnet/blazor-samples)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 👥 Credits

- **Original Idea**: Pablo Piovano ([LinkedIn](https://www.linkedin.com/in/ppiova/))
- **Implementation**: Bruno Capuano
- **Powered By**: Azure AI Services, .NET 9, Blazor

## 🆘 Support

For questions or issues:
1. Check the [Troubleshooting](#-troubleshooting) section
2. Review [Azure documentation](https://docs.microsoft.com/azure/)
3. Open an issue on GitHub
4. Contact the maintainers

---

**Built with ❤️ using .NET 9 and Azure AI**
