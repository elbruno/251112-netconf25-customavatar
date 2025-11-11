# Azure AI Avatar Demo

An interactive talking avatar application powered by Azure AI Services. This repository showcases how to build conversational AI experiences using Azure Speech Services (Speech-to-Text, Text-to-Speech, and Avatar) combined with Azure OpenAI for intelligent responses.

![License](https://img.shields.io/badge/license-MIT-blue.svg)

## 🎯 What is This?

This project demonstrates how to create a **talking avatar** that can:
- 👂 Listen to your voice (Speech-to-Text)
- 🤖 Generate intelligent responses (Azure OpenAI)
- 🗣️ Speak back with natural voice (Text-to-Speech)
- 👤 Display synchronized lip movements (Avatar video streaming)

The avatar appears in a video window and responds to your questions in real-time, creating an immersive conversational experience.

## 🚀 Who is This For?

- **Developers** exploring Azure AI capabilities
- **Solution Architects** designing conversational AI systems
- **Product Teams** building customer service avatars, virtual assistants, or educational tools
- **Students** learning about Azure AI Services integration

## 📦 What's Included?

This repository includes **two complete implementations** of the same application:

### 1. JavaScript/HTML Implementation ⚡
**Location**: `python/` folder + root HTML files

**Best for:**
- Quick prototyping and demos
- Web-based deployments
- Learning Azure Speech SDK basics
- Static hosting (GitHub Pages, Azure Static Web Apps)

**Tech Stack:**
- Vanilla JavaScript
- HTML5 + Bootstrap 5
- Azure Speech SDK (CDN)
- Express.js dev server

[👉 Get Started with JavaScript →](python/README.md)

### 2. .NET 9 Blazor with Aspire 🔥
**Location**: `dotnet/` folder

**Best for:**
- Production enterprise applications
- Cloud-native deployments
- Teams using .NET ecosystem
- Applications requiring observability and telemetry

**Tech Stack:**
- .NET 9 + Blazor Server
- .NET Aspire for orchestration
- Azure SDK for .NET
- OpenTelemetry built-in

[👉 Get Started with .NET →](dotnet/README.md)

## 🎬 Quick Start

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
