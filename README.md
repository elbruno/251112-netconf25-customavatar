# Welcome to the Azure AI Avatar Agent

## What is This Project?

The Azure AI Avatar Agent is a cutting-edge conversational AI application that brings together voice, intelligence, and visual presence to create truly immersive interactions. Imagine chatting with an AI that doesn't just respond with text, but speaks to you with a natural voice while displaying synchronized facial movements and lip movements through a talking avatar.

This project demonstrates how to build modern, cloud-native AI applications using Azure AI Services and .NET technologies.

## What Can It Do?

The AI Avatar Agent provides a complete conversational experience:

- **👂 Listen to You**: Converts your spoken words into text using Azure Speech-to-Text
- **🤖 Think Intelligently**: Generates contextual responses using Azure OpenAI's GPT models
- **🗣️ Speak Back**: Synthesizes natural-sounding speech with Azure Text-to-Speech
- **👤 Show Emotions**: Displays a realistic avatar with synchronized lip movements as it speaks
- **💬 Remember Context**: Maintains conversation history for coherent dialogues
- **🌍 Support Multiple Languages**: Recognizes and responds in 40+ languages

## The .NET Implementation

This repository includes **two complete implementations** of the same application. This guide focuses on the **.NET 10 Blazor implementation** with .NET Aspire 13.0 orchestration, which is designed for enterprise and production scenarios.

### Why Choose the .NET Implementation?

The .NET version is ideal if you need:

✅ **Enterprise-grade reliability** with built-in resilience patterns  
✅ **Production observability** with OpenTelemetry for logs, metrics, and traces  
✅ **One-command deployment** to Azure with automatic resource provisioning  
✅ **Secure secret management** with managed identities (no API keys in production!)  
✅ **Developer productivity** with real-time monitoring via Aspire Dashboard  
✅ **Type safety and performance** that comes with compiled .NET code

## 🤖 Three Operational Modes

The .NET application supports **three different modes** for AI conversations, each designed for specific use cases:

### Mode 1: Chat with LLM (Default)

**Direct integration with Azure OpenAI**

- Uses standard Azure OpenAI chat completion API
- Simple request/response pattern with streaming support
- Best for: Quick demos, simple conversations, direct LLM access
- Configuration: Requires Azure OpenAI endpoint, API key, and deployment name

### Mode 2: Chat with Agent-LLM

**Microsoft Agent Framework with Azure OpenAI**

- Uses [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/) with standard LLM
- Enables agent-based workflows and orchestration patterns
- Supports tool calling, memory, and advanced agent behaviors
- Best for: Complex workflows, multi-step reasoning, tool integration
- Configuration: Same as Mode 1 + Agent Framework settings

### Mode 3: Chat with Azure AI Foundry Agent

**Pre-built agents from Azure AI Foundry**

- Connects to agents already created in [Azure AI Foundry](https://ai.azure.com)
- Uses Microsoft Agent Framework with pre-configured agent endpoints
- Supports grounding, RAG (Retrieval Augmented Generation), and custom tools
- Best for: Production scenarios, enterprise agents, grounded responses
- Configuration: Requires AI Foundry project ID, agent ID, and managed identity/key
- Learn more: [Azure AI Foundry Agent Types](https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/agent-types/azure-ai-foundry-agent?pivots=programming-language-csharp)

> **💡 Tip**: Switch between modes in the Configuration page (`/config`) under "Azure OpenAI / Agent Configuration" → "Mode" dropdown

## How Does It Work?

### The Big Picture

Here's what happens when you have a conversation with the AI Avatar:

```text
┌─────────────────────────────────────────────────────────────────┐
│                         YOUR BROWSER                             │
│                                                                  │
│  ┌──────────────┐         ┌──────────────┐                     │
│  │   Chat UI    │◄───────►│ Avatar Video │                     │
│  │  (Blazor)    │         │   WebRTC     │                     │
│  └──────┬───────┘         └──────▲───────┘                     │
│         │                        │                              │
└─────────┼────────────────────────┼──────────────────────────────┘
          │                        │
          │ SignalR                │ WebRTC Stream
          │                        │
┌─────────▼────────────────────────┼──────────────────────────────┐
│                 .NET 10 BLAZOR SERVER                            │
│                                  │                               │
│  ┌───────────────────────────────┼─────────────────────────┐   │
│  │           APPLICATION SERVICES│                         │   │
│  │                              │                         │   │
│  │  ┌────────────┐  ┌──────────▼──────┐  ┌────────────┐  │   │
│  │  │  OpenAI    │  │     Speech      │  │   Config   │  │   │
│  │  │  Service   │  │     Service     │  │   Service  │  │   │
│  │  └─────┬──────┘  └─────────┬───────┘  └────────────┘  │   │
│  └────────┼───────────────────┼──────────────────────────┘   │
│           │                   │                               │
└───────────┼───────────────────┼───────────────────────────────┘
            │                   │
    ┌───────┼───────────────────┼───────┐
    │       │                   │       │
┌───▼───────▼────┐   ┌──────────▼───────▼──┐
│  Azure OpenAI  │   │   Azure Speech      │
│                │   │     Service         │
│  • GPT Models  │   │   • STT/TTS         │
│  • Streaming   │   │   • Avatar Video    │
└────────────────┘   └─────────────────────┘
```

### Step-by-Step Flow

Let's walk through a typical conversation:

**1. You speak into your microphone**

- Your browser captures the audio
- Azure Speech Service converts it to text (Speech-to-Text)

**2. The text is sent to Azure OpenAI or Agent**

- Your message is added to the conversation history
- Depending on mode: Azure OpenAI (Mode 1), Agent Framework with LLM (Mode 2), or AI Foundry Agent (Mode 3) generates an intelligent response
- The response is streamed back in real-time

**3. The AI response is converted to speech**

- Text flows to Azure Text-to-Speech
- A natural voice reads the response
- Simultaneously, the avatar synthesizer generates video

**4. You see and hear the avatar**

- WebRTC streams the avatar video to your browser
- Lip movements sync perfectly with the speech
- You see the avatar "speaking" to you in real-time

## Getting Started

### Prerequisites

Before you begin, ensure you have:

1. **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** installed
2. **.NET Aspire workload**: Run `dotnet workload install aspire`
3. **Azure Subscription** - [Create a free account](https://azure.microsoft.com/free/)
4. **Azure OpenAI access** - [Request access](https://aka.ms/oai/access)

### 5-Minute Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/elbruno/customavatarlabs.git
cd customavatarlabs/dotnet

# 2. Install Aspire workload (if not done)
dotnet workload install aspire

# 3. Configure your Azure credentials
cd AzureAIAvatarBlazor.AppHost

# Required for all modes
dotnet user-secrets set "ConnectionStrings:speech" \
  "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_KEY;"
dotnet user-secrets set "Avatar:Character" "lisa"

# For Mode 1 (LLM) - Default mode
dotnet user-secrets set "ConnectionStrings:openai" \
  "Endpoint=https://YOUR_RESOURCE.openai.azure.com/;Key=YOUR_KEY;"
dotnet user-secrets set "OpenAI:DeploymentName" "gpt-4o-mini"
dotnet user-secrets set "Agent:Mode" "LLM"

# For Mode 2 (Agent-LLM) - uncomment and use instead
# dotnet user-secrets set "Agent:Mode" "Agent-LLM"

# For Mode 3 (Agent-AIFoundry) - uncomment and use instead
# dotnet user-secrets set "ConnectionStrings:aifoundry" \
#   "Endpoint=https://YOUR_PROJECT.api.azureml.ms/;Key=YOUR_KEY;"
# dotnet user-secrets set "Agent:Mode" "Agent-AIFoundry"
# dotnet user-secrets set "Agent:AgentId" "YOUR_AGENT_ID"

# 4. Run the application
dotnet run

# 5. Open your browser
# - Aspire Dashboard: https://localhost:15216
# - Blazor App: https://localhost:5001
```

### What You'll See

**Aspire Dashboard** (<https://localhost:15216>):

- Real-time logs from your application
- Performance metrics and traces
- Health status of all services
- Distributed tracing visualization

**Blazor Application** (<https://localhost:5001>):

- Home page with project overview
- Configuration page to manage settings (switch between 3 modes here!)
- Chat page with interactive avatar

## Next Steps

### Learn More

1. **[Quick Start Guide](dotnet/docs/QUICKSTART.md)** - Get running in 5 minutes
2. **[Architecture Deep Dive](dotnet/docs/ARCHITECTURE.md)** - Technical details
3. **[Deployment Guide](dotnet/docs/DEPLOYMENT.md)** - Production deployment options
4. **[Full .NET README](dotnet/README.md)** - Complete documentation

### For JavaScript Implementation

This repository also includes a JavaScript/HTML version:

- **[JavaScript Implementation Guide](python/README.md)** - Lightweight web version
- Best for quick prototyping and demos
- No backend server needed

### Customize Your Avatar

- Explore different avatar characters and styles
- Adjust system prompts for different personalities
- Configure audio gain for optimal volume
- Enable subtitles for accessibility
- Switch between the three operational modes for different use cases

### Contribute

We welcome contributions! Check out:

- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Contribution guidelines
- **[GitHub Issues](https://github.com/elbruno/customavatarlabs/issues)** - Bug reports and feature requests
- **[GitHub Discussions](https://github.com/elbruno/customavatarlabs/discussions)** - Questions and ideas

## Credits and Acknowledgments

This project is built with:

- **[.NET 10](https://dotnet.microsoft.com/)** - Application framework
- **[.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)** - Cloud-native orchestration
- **[Azure OpenAI](https://azure.microsoft.com/products/ai-services/openai-service)** - GPT models
- **[Azure AI Foundry](https://ai.azure.com)** - Pre-built AI agents
- **[Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/)** - Agent orchestration
- **[Azure Speech Service](https://azure.microsoft.com/products/ai-services/ai-speech)** - Voice and avatar
- **[Azure MCP Server](https://aka.ms/azmcp/ga)** - Model Context Protocol integration
- **[Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)** - Interactive web UI

**Created by**:

- **Pablo Piovano** ([LinkedIn](https://www.linkedin.com/in/ppiova/)) - Original concept
- **Bruno Capuano** ([GitHub](https://github.com/elbruno) | [LinkedIn](https://www.linkedin.com/in/brunocapuano/)) - Implementation

**License**: [MIT License](LICENSE)

---

**Ready to get started?** Follow the [Quick Start Guide](dotnet/docs/QUICKSTART.md) and have your AI avatar running in 5 minutes!

For the JavaScript/HTML version, see the [Python folder README](python/README.md).

### Prerequisites

Before you begin, you'll need:

1. **Azure Subscription** - [Create free account](https://azure.microsoft.com/free/)
2. **Azure Services:**
   - [Azure Speech Service](https://portal.azure.com/#create/Microsoft.CognitiveServicesSpeechServices) - For STT/TTS and Avatar
   - [Azure OpenAI Service](https://portal.azure.com/#create/Microsoft.CognitiveServicesOpenAI) - For chat intelligence
3. **Development Tools** (choose based on implementation):
   - JavaScript: Node.js 18+ and a modern browser
   - .NET: .NET 9 SDK and `dotnet workload install aspire`

### Choose Your Path

<table>
<tr>
<td width="50%">

#### JavaScript Quick Start

```bash
# Clone the repository
git clone https://github.com/elbruno/customavatarlabs.git
cd customavatarlabs

# Copy and configure environment
cp python/.env.example python/.env
# Edit python/.env with your Azure credentials

# Start dev server
cd python/dev-server
npm install
npm start

# Open browser
# http://localhost:5173/config.html
```

[📖 Full JavaScript Guide →](python/README.md)

</td>
<td width="50%">

#### .NET Quick Start

```bash
# Clone the repository
git clone https://github.com/elbruno/customavatarlabs.git
cd customavatarlabs/dotnet

# Install Aspire workload
dotnet workload install aspire

# Configure secrets (AppHost)
cd AzureAIAvatarBlazor.AppHost
dotnet user-secrets set "ConnectionStrings:openai" \
  "Endpoint=https://YOUR_RESOURCE.openai.azure.com/;Key=YOUR_KEY;"
dotnet user-secrets set "ConnectionStrings:speech" \
  "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_KEY;"

# Run with Aspire
dotnet run
# OR press Ctrl+Shift+B in VS Code

# Open browser
# https://localhost:5001 (Blazor App)
# https://localhost:15216 (Aspire Dashboard)
```

[📖 Full .NET Guide →](dotnet/README.md)

</td>
</tr>
</table>

## 🌟 Key Features

### Core Capabilities

- ✅ **Real-time Avatar Video** - WebRTC-based streaming with lip sync
- ✅ **Multi-language Support** - Automatic language detection for 40+ languages
- ✅ **Custom Avatars** - Use built-in avatars or your own custom characters
- ✅ **Voice Customization** - Select from 400+ neural voices or use custom voices
- ✅ **Streaming Responses** - Real-time AI responses as the avatar speaks
- ✅ **Persistent Configuration** - Save your settings locally or in environment variables
- ✅ **Audio Gain Control** - Adjust volume from 0.1x to 5.0x
- ✅ **Subtitle Support** - Display text captions during speech

### Advanced Features

- 🎯 **Prompt Profiles** - Pre-configured AI personalities and behaviors
- 🔄 **Auto-reconnect** - Automatically recover from connection issues
- 📊 **On Your Data** - Connect to Azure Cognitive Search for custom knowledge bases
- 🎨 **Customizable UI** - Bootstrap-based themes (Microsoft/Fluent style included)
- 📝 **Conversation History** - View and manage chat history
- 🔊 **Mixed Input** - Type messages or use voice input

## 📚 Documentation

### Getting Started

- [JavaScript Implementation Guide](python/README.md) - Setup, configuration, and deployment
- [.NET Blazor Implementation Guide](dotnet/README.md) - Aspire setup and enterprise features
- [Prompt Profiles Guide](python/prompts/README-PROFILES.md) - Customize AI behavior

### .NET Documentation

- [Quick Start](dotnet/docs/QUICKSTART.md) - Get running in 5 minutes
- [Architecture](dotnet/docs/ARCHITECTURE.md) - Technical architecture and design
- [Deployment Guide](dotnet/docs/DEPLOYMENT.md) - Production deployment options
- [Migration Docs](dotnet/docs/migration/) - Aspire migration details (for maintainers)

### Configuration

Both implementations support flexible configuration:

```bash
# Azure Speech Service
AZURE_SPEECH_REGION=westus2
AZURE_SPEECH_API_KEY=your_key_here

# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
AZURE_OPENAI_API_KEY=your_key_here
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini

# Avatar Settings
AVATAR_CHARACTER=lisa
AVATAR_STYLE=casual-sitting
TTS_VOICE=en-US-AvaMultilingualNeural

# System Prompt
SYSTEM_PROMPT=You are a helpful AI assistant.
```

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────┐
│                  User Interface                  │
│         (Browser - JavaScript or Blazor)         │
└─────────────────┬───────────────────────────────┘
                  │
        ┌─────────┴─────────┐
        │                   │
   ┌────▼────┐       ┌──────▼──────┐
   │  Azure  │       │    Azure    │
   │ Speech  │       │   OpenAI    │
   │ Service │       │   Service   │
   └─────────┘       └─────────────┘
        │                   │
   ┌────▼────────────────────▼────┐
   │      WebRTC Connection       │
   │   (Avatar Video Streaming)   │
   └──────────────────────────────┘
```

### Data Flow

1. **User speaks** → Azure Speech STT → Text
2. **Text** → Azure OpenAI → AI Response
3. **AI Response** → Azure Speech TTS → Audio
4. **Audio + Avatar** → WebRTC → Video Stream
5. **Video Stream** → Browser → User sees talking avatar

## 🚢 Deployment Options

### JavaScript Implementation

- **Static Hosting**: Azure Static Web Apps, GitHub Pages, Netlify, Vercel
- **Container**: Docker + Azure Container Apps or App Service
- **CDN**: Azure CDN for global distribution

### .NET Implementation

- **Azure App Service**: Traditional web hosting
- **Azure Container Apps**: Containerized deployment with scaling
- **Azure Kubernetes Service (AKS)**: For complex orchestration
- **Aspire Deployment**: One-command deployment with `azd up`

[📖 Detailed Deployment Guide →](dotnet/docs/DEPLOYMENT.md)

## 🔒 Security Best Practices

⚠️ **Important**: Never commit API keys or secrets to source control!

### Recommended Approaches

1. **Development**:
   - JavaScript: Use `.env` files (git-ignored)
   - .NET: Use User Secrets (`dotnet user-secrets set`)

2. **Production**:
   - Use Azure Key Vault for secret storage
   - Use Managed Identities for authentication (no keys needed!)
   - Set secrets as environment variables in your hosting platform

### What's Protected

- `.env` files are in `.gitignore`
- `appsettings.Development.json` is excluded
- User secrets are stored outside the project directory

## 🐛 Troubleshooting

### Common Issues

<details>
<summary><b>Avatar video won't load</b></summary>

**Causes:**

- Invalid Speech Service credentials
- Wrong region configuration
- Browser doesn't support WebRTC
- Firewall blocking WebRTC connections

**Solutions:**

1. Verify your Speech Service key and region in Azure Portal
2. Test with Chrome or Edge (best WebRTC support)
3. Check browser console (F12) for JavaScript errors
4. Disable browser extensions temporarily

</details>

<details>
<summary><b>No audio from avatar</b></summary>

**Causes:**

- Audio gain set too low
- Browser autoplay policy blocking audio
- Audio output device issues

**Solutions:**

1. Increase Audio Gain in configuration (try 1.5x - 2.0x)
2. Click anywhere on the page to resume audio context
3. Check system audio output settings
4. Try in a different browser

</details>

<details>
<summary><b>Chat not responding</b></summary>

**Causes:**

- Invalid OpenAI credentials
- Model not deployed in Azure
- Rate limit exceeded
- Network connectivity issues

**Solutions:**

1. Verify OpenAI endpoint, key, and deployment name
2. Check deployment status in Azure Portal
3. Wait a few minutes if rate limited
4. Test connection using the "Test OpenAI Connection" button

</details>

<details>
<summary><b>403 errors with custom avatar</b></summary>

**Cause:** Mixing custom avatar with custom voice endpoint

**Solution:** Enable "Use Built-In Voice" checkbox, or clear the Custom Voice Endpoint ID field
</details>

## 🤝 Contributing

Contributions are welcome! Here's how you can help:

1. **Report Bugs**: Open an issue with detailed reproduction steps
2. **Suggest Features**: Share your ideas in discussions
3. **Submit PRs**: Fork, create a feature branch, and submit a pull request
4. **Improve Docs**: Help make documentation clearer and more complete

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Credits

- **Original Idea & Design**: Pablo Piovano ([LinkedIn](https://www.linkedin.com/in/ppiova/))
- **Implementation**: Bruno Capuano ([GitHub](https://github.com/elbruno)) ([LinkedIn](https://www.linkedin.com/in/brunocapuano/))
- **Powered By**: Microsoft Azure AI Services

## 🆘 Support

Need help? Try these resources:

1. **Documentation**: Check the guides above
2. **Issues**: [GitHub Issues](https://github.com/elbruno/customavatarlabs/issues)
3. **Azure Docs**:
   - [Azure Speech Service](https://docs.microsoft.com/azure/cognitive-services/speech-service/)
   - [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/)
4. **Community**: Azure Developer Community forums

## 🌟 Show Your Support

If you find this project helpful:

- ⭐ Star this repository
- 🐦 Share on social media
- 📝 Write a blog post about your experience
- 🔗 Reference it in your projects

## 📈 What's Next?

### Potential Enhancements

- [ ] Multi-user support with sessions
- [ ] Voice cloning integration
- [ ] Mobile app versions (iOS/Android)
- [ ] Real-time translation between languages
- [ ] Avatar customization editor
- [ ] Background/scene customization
- [ ] Emotion detection and expression

Want to contribute? Pick an enhancement and start a discussion!

---

**Built with ❤️ using Azure AI Services**

[JavaScript Guide](python/README.md) | [.NET Guide](dotnet/README.md) | [Issues](https://github.com/elbruno/customavatarlabs/issues) | [License](LICENSE)
