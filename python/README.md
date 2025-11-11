# Azure AI Avatar - JavaScript/HTML Implementation

A browser-based talking avatar application built with vanilla JavaScript, HTML5, and Azure AI Services. This implementation is perfect for quick demos, web-based deployments, and learning Azure Speech SDK fundamentals.

## 🎯 Overview

This is the **JavaScript/HTML implementation** of the Azure AI Avatar demo. It features:
- Zero backend dependencies (runs entirely in the browser)
- Direct integration with Azure Speech SDK via CDN
- Static file deployment (GitHub Pages, Azure Static Web Apps, etc.)
- Local development server with `.env` support
- Microsoft/Fluent-inspired UI design

## 📋 Prerequisites

### Required Azure Services
1. **Azure Speech Service**
   - [Create in Azure Portal](https://portal.azure.com/#create/Microsoft.CognitiveServicesSpeechServices)
   - Note your API Key and Region (e.g., `westus2`)
   - Avatar feature must be enabled

2. **Azure OpenAI Service**
   - [Create in Azure Portal](https://portal.azure.com/#create/Microsoft.CognitiveServicesOpenAI)
   - Deploy a chat model (e.g., `gpt-4o-mini`, `gpt-4`)
   - Note your Endpoint, API Key, and Deployment Name

3. **Azure Cognitive Search** (Optional - for "On Your Data")
   - Create a search service
   - Create and populate an index
   - Note Endpoint, API Key, and Index Name

### Development Environment
- **Node.js** 18+ (for the dev server)
- **Modern Browser**: Chrome, Edge, Firefox, or Safari
- **Text Editor**: VS Code recommended

## 🚀 Quick Start

### Option A: File-based (No Server)

Perfect for quick testing without setup:

```bash
# 1. Clone the repository
git clone https://github.com/elbruno/customavatarlabs.git
cd customavatarlabs

# 2. Open config.html directly in your browser
# File → Open → config.html

# 3. Import your .env file using the UI
# Click "Import .env (local file)" button
# Select your .env file
# Click "Save Configuration"

# 4. Navigate to chat.html
# File → Open → chat.html
# Click "Open Avatar Session"
```

⚠️ **Note**: The auto-load of `/.env.json` is disabled on `file://` protocol to avoid CORS errors.

### Option B: Dev Server (Recommended)

Provides automatic `.env` loading and HTTPS support:

```bash
# 1. Clone the repository
git clone https://github.com/elbruno/customavatarlabs.git
cd customavatarlabs

# 2. Create and configure .env file
cp python/.env.example python/.env
# Edit python/.env with your Azure credentials

# 3. Install dev server dependencies
cd python/dev-server
npm install

# 4. Start the server
npm start
# Server starts at http://localhost:5173

# 5. Open in browser
# http://localhost:5173/config.html (configure first)
# http://localhost:5173/chat.html (start chatting)
```

### Option C: VS Code Tasks (Easiest)

Bypass PowerShell execution policy issues:

```bash
# 1. Open repository in VS Code
code customavatarlabs

# 2. Run task
# Press Ctrl+Shift+P
# Type: "Tasks: Run Task"
# Select: "Dev Server (HTTP)" or "Dev Server (HTTPS)"

# 3. Server starts automatically
# Open http://localhost:5173/config.html
```

## ⚙️ Configuration

### Environment Variables

Create a `.env` file in the `python/` directory:

```bash
# Azure Speech Service
AZURE_SPEECH_API_KEY=your_speech_key
AZURE_SPEECH_REGION=westus2
AZURE_SPEECH_PRIVATE_ENDPOINT=https://your-name.cognitiveservices.azure.com/

# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
AZURE_OPENAI_API_KEY=your_openai_key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini
SYSTEM_PROMPT=You are a helpful AI assistant.

# Prompt Profiles (optional)
PROMPT_PROFILE=general-assistant
PROMPT_ENFORCE_PROFILE=false
PROMPT_VAR_tone=helpful
PROMPT_VAR_format=concise

# Azure Cognitive Search (optional)
AZURE_COGNITIVE_SEARCH_ENDPOINT=https://your-search.search.windows.net
AZURE_COGNITIVE_SEARCH_API_KEY=your_search_key
AZURE_COGNITIVE_SEARCH_INDEX_NAME=your_index

# TTS / Avatar Settings
TTS_VOICE=en-US-AvaMultilingualNeural
CUSTOM_VOICE_ENDPOINT_ID=
AVATAR_CHARACTER=lisa
AVATAR_STYLE=casual-sitting
ENABLE_CONTINUOUS_CONVERSATION=false
ENABLE_SUBTITLES=true
ENABLE_AUTO_RECONNECT=true
```

### Using the Configuration UI

1. **Open config.html** in your browser
2. **Configure each section:**

   **Azure Speech**
   - Region: Your Speech resource region
   - API Key: Your subscription key
   - Private Endpoint (optional): Custom endpoint URL
   - Test connection button to verify

   **Azure OpenAI**
   - Endpoint: Your OpenAI resource URL
   - API Key: Your subscription key
   - Deployment Name: Your deployed model name
   - System Prompt: Customize AI behavior
   - Test connection button to verify

   **STT/TTS**
   - STT Locales: Comma-separated list (e.g., `en-US,es-ES,fr-FR`)
   - TTS Voice: Select from dropdown or enter custom
   - Custom Voice Endpoint ID: For custom neural voices
   - Audio Gain: Volume multiplier (0.1x - 5.0x)

   **Avatar**
   - Character: Select from dropdown (Lisa, Harry, Jeff, etc.)
   - Style: Auto-filtered based on character
   - Custom Avatar: Enable for custom characters
   - Use Built-In Voice: For custom avatars with built-in voices
   - Subtitles: Show text during speech
   - Auto-Reconnect: Automatically recover from errors

3. **Save Configuration** - Stored in browser localStorage

### Prompt Profiles

Prompt profiles allow you to create reusable AI personalities. See [prompts/README-PROFILES.md](prompts/README-PROFILES.md) for details.

**Create a Profile:**

1. Create `prompts/my-profile.md`:
```markdown
You are {{name}}, a {{role}}.

Your tone is {{tone}}.
Keep responses {{format}}.
```

2. Add to `prompts/index.json`:
```json
{
  "profiles": [
    {
      "id": "my-profile",
      "name": "My Custom Profile",
      "file": "my-profile.md",
      "defaults": {
        "name": "Alex",
        "role": "helpful assistant",
        "tone": "friendly",
        "format": "concise"
      }
    }
  ]
}
```

3. Use in `.env`:
```bash
PROMPT_PROFILE=my-profile
PROMPT_ENFORCE_PROFILE=true
PROMPT_VAR_tone=enthusiastic
```

## 💬 Using the Chat Interface

### Starting a Session

1. Open `chat.html` in your browser
2. Click **"Open Avatar Session"**
3. Wait for the avatar video to load (may take 5-10 seconds)
4. Avatar appears and is ready to interact

### Sending Messages

**Option 1: Voice Input**
1. Click **"Start Microphone"**
2. Speak your question
3. Avatar responds with voice and lip sync

**Option 2: Text Input**
1. Enable **"Type Message"** checkbox
2. Type your message in the input box
3. Press **Enter** or click **"Send"**
4. Avatar speaks the response

### Chat Layout

- **Left Panel (Video)**: Avatar video stream
- **Right Panel (Conversation)**: 
  - Chat history (scrollable)
  - Message input
  - Control buttons

### Control Buttons

- **Stop Speaking**: Interrupt the avatar
- **Clear History**: Reset conversation (keeps system prompt)
- **Close Session**: Disconnect avatar session

## 🔧 Development Server Details

### Server Features

The dev server in `python/dev-server/` provides:

1. **Static File Serving**
   - Serves `python/` folder first (for assets)
   - Falls back to root folder (for HTML files)

2. **`.env` Endpoint**
   - Exposes `.env` as JSON at `/.env.json`
   - Only accessible locally (for development)
   - Never use in production!

3. **Prompt Profiles API**
   - Lists profiles at `/api/prompts`
   - Serves templates from `python/prompts/`

4. **HTTPS Support**
   - Auto-detects certificates in `certs/` folder
   - Falls back to HTTP if certs not found

### HTTPS Setup (Optional)

Some features require HTTPS. Generate trusted localhost certificates:

**Windows (mkcert):**
```powershell
# Install mkcert
choco install mkcert -y

# Install local CA (one-time)
mkcert -install

# Generate certificates
cd python/dev-server/certs
mkcert -key-file localhost-key.pem -cert-file localhost.pem localhost 127.0.0.1 ::1

# Start server (auto-uses HTTPS)
cd ..
npm start
```

**macOS (mkcert):**
```bash
# Install mkcert
brew install mkcert
mkcert -install

# Generate certificates
cd python/dev-server/certs
mkcert -key-file localhost-key.pem -cert-file localhost.pem localhost 127.0.0.1 ::1

# Start server
cd ..
npm start
```

**Linux (mkcert):**
```bash
# Install mkcert
sudo apt install libnss3-tools
wget https://github.com/FiloSottile/mkcert/releases/download/v1.4.4/mkcert-v1.4.4-linux-amd64
chmod +x mkcert-v1.4.4-linux-amd64
sudo mv mkcert-v1.4.4-linux-amd64 /usr/local/bin/mkcert
mkcert -install

# Generate certificates
cd python/dev-server/certs
mkcert -key-file localhost-key.pem -cert-file localhost.pem localhost 127.0.0.1 ::1

# Start server
cd ..
npm start
```

### PowerShell Execution Policy

If you see "running scripts is disabled" error:

**Temporary (Process Only):**
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
cd python\dev-server
npm start
```

**VS Code Task (Recommended):**
- Press `Ctrl+Shift+P`
- Select "Tasks: Run Task"
- Choose "Dev Server (HTTP)" or "Dev Server (HTTPS)"
- Bypasses policy automatically

## 🏗️ Project Structure

```
python/
├── dev-server/              # Local development server
│   ├── server.js           # Express server with .env endpoint
│   ├── package.json        # Dependencies
│   └── certs/              # HTTPS certificates (git-ignored)
├── js/                     # Application JavaScript
│   ├── chat.js            # Avatar session and chat logic
│   ├── config.js          # Configuration UI and management
│   └── basic.js           # Legacy demo
├── css/                    # Stylesheets
│   ├── styles.css         # Base styles
│   ├── theme-microsoft.css # Microsoft/Fluent theme
│   └── theme-system.css   # CSS variables
├── prompts/               # Prompt profiles
│   ├── index.json        # Profile registry
│   ├── README-PROFILES.md # Documentation
│   └── *.md              # Profile templates
├── avatar/               # Avatar assets
├── image/                # UI images
├── video/                # Video assets
└── .env.example          # Example configuration

Root:
├── chat.html            # Chat interface
├── config.html          # Configuration UI
├── basic.html           # Legacy demo
└── test.html            # Test page
```

## 🚢 Deployment Options

### Static Hosting

Deploy as static files to any hosting platform:

**Azure Static Web Apps:**
```bash
# Install Azure Static Web Apps CLI
npm install -g @azure/static-web-apps-cli

# Deploy
cd customavatarlabs
swa deploy \
  --app-location "/" \
  --output-location "/" \
  --api-location "python/dev-server"
```

**GitHub Pages:**
```bash
# Push to gh-pages branch
git checkout -b gh-pages
git add -f python/* *.html
git commit -m "Deploy to GitHub Pages"
git push origin gh-pages

# Enable in GitHub Settings → Pages
# Source: gh-pages branch
```

**Netlify:**
```bash
# Install Netlify CLI
npm install -g netlify-cli

# Deploy
netlify deploy \
  --dir . \
  --prod
```

**Vercel:**
```bash
# Install Vercel CLI
npm install -g vercel

# Deploy
vercel --prod
```

### Container Deployment

**Dockerfile:**
```dockerfile
FROM node:18-alpine
WORKDIR /app
COPY . .
WORKDIR /app/python/dev-server
RUN npm install
EXPOSE 5173
CMD ["node", "server.js"]
```

**Build and Run:**
```bash
docker build -t avatar-demo .
docker run -p 5173:5173 \
  -v $(pwd)/python/.env:/app/python/.env \
  avatar-demo
```

**Azure Container Apps:**
```bash
# Build and push
az acr build --registry myregistry --image avatar-demo:latest .

# Deploy
az containerapp create \
  --name avatar-demo \
  --resource-group mygroup \
  --image myregistry.azurecr.io/avatar-demo:latest \
  --target-port 5173 \
  --ingress external \
  --environment-variables-file env.yaml
```

## 🐛 Troubleshooting

### Avatar Connection Issues

**Problem**: "Failed to get avatar token"
- **Check**: Speech Service key and region are correct
- **Check**: Avatar feature is enabled in Speech resource
- **Try**: Test connection button in config.html

**Problem**: Avatar video is black or won't load
- **Check**: Browser supports WebRTC (Chrome, Edge, Firefox, Safari)
- **Check**: Network allows WebRTC (check firewall)
- **Try**: Open browser console (F12) for error messages

### Audio Issues

**Problem**: No audio from avatar
- **Check**: Audio Gain setting (try 1.5x - 2.0x)
- **Check**: Browser autoplay policy (click page to resume)
- **Check**: System audio output settings
- **Try**: Different browser

**Problem**: Audio is too quiet
- **Solution**: Increase Audio Gain in config.html
- **Range**: 0.1x (very quiet) to 5.0x (very loud)
- **Recommended**: 1.5x - 2.0x

### Custom Avatar Issues

**Problem**: 403 errors or silence with custom avatar
- **Cause**: Mixing custom avatar with custom voice endpoint
- **Solution**: Enable "Use Built-In Voice" checkbox
- **Solution**: Clear "Custom Voice Endpoint ID" field

**Problem**: 1007 "not owned" error
- **Check**: Avatar exists in your Speech resource
- **Check**: Avatar is in the same region
- **Check**: You own the custom avatar

### OpenAI Issues

**Problem**: Chat not responding
- **Check**: OpenAI endpoint, key, and deployment name
- **Check**: Model is deployed and active
- **Try**: Test connection button in config.html
- **Try**: Check quota and rate limits

**Problem**: 429 throttling errors
- **Cause**: Rate limit exceeded
- **Solution**: Wait ~30 seconds and retry
- **Solution**: Don't start multiple sessions simultaneously

### CORS Issues

**Problem**: CORS errors loading `.env.json`
- **Expected**: Only on `file://` protocol
- **Solution**: Use the dev server (`npm start`)
- **Solution**: Use "Import .env" button instead

### Dev Server Issues

**Problem**: "Cannot find module" errors
- **Solution**: Run `npm install` in `python/dev-server/`

**Problem**: "Port already in use"
- **Solution**: Kill process on port 5173
- **Solution**: Set custom port: `PORT=5174 npm start`

**Problem**: PowerShell script execution error
- **Solution**: Use VS Code task (bypasses policy)
- **Solution**: Run with Node directly: `node server.js`

## 🎨 Customization

### Change Theme

Edit `css/theme-microsoft.css`:

```css
:root {
  --primary-color: #0078d4;      /* Change primary color */
  --secondary-color: #005a9e;    /* Change secondary color */
  --background-color: #f3f2f1;   /* Change background */
  --text-color: #323130;         /* Change text color */
}
```

### Add Custom Avatar

Edit `js/config.js`:

```javascript
const avatarCharacters = [
  // ... existing avatars
  {
    character: 'myavatar',
    label: 'My Custom Avatar',
    styles: ['casual', 'formal']
  }
];
```

### Add Custom Voice

Edit `js/config.js`:

```javascript
const popularVoices = [
  // ... existing voices
  { value: 'en-US-MyCustomVoice', label: 'My Custom Voice' }
];
```

### Modify Layout

Edit `chat.html`:

```html
<!-- Change column widths -->
<div class="col-lg-8">  <!-- Change to col-lg-6 for smaller video -->
  <!-- Avatar video -->
</div>
<div class="col-lg-4">  <!-- Change to col-lg-6 for larger chat -->
  <!-- Conversation panel -->
</div>
```

## 🔒 Security Notes

### Local Development
- ✅ `.env` file is git-ignored
- ✅ Dev server is for local use only
- ✅ Never commit API keys or secrets

### Production Deployment
- ⚠️ **Don't use dev server** in production
- ⚠️ **Don't expose `.env.json` endpoint**
- ✅ Use Azure Key Vault for secrets
- ✅ Use Managed Identities when possible
- ✅ Set secrets as environment variables in hosting platform

### API Key Protection
```javascript
// ❌ DON'T: Hardcode keys in JavaScript
const apiKey = 'your-key-here';

// ✅ DO: Load from configuration
const apiKey = config.azureSpeech.apiKey;

// ✅ BETTER: Use backend proxy
fetch('/api/avatar-token')  // Backend handles credentials
```

## 📚 Additional Resources

### Azure Documentation
- [Azure Speech Service](https://docs.microsoft.com/azure/cognitive-services/speech-service/)
- [Avatar API Reference](https://docs.microsoft.com/azure/cognitive-services/speech-service/avatar)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [Speech SDK for JavaScript](https://docs.microsoft.com/azure/cognitive-services/speech-service/quickstarts/setup-platform?pivots=programming-language-javascript)

### Code Samples
- [Azure Speech SDK Samples](https://github.com/Azure-Samples/cognitive-services-speech-sdk)
- [Azure OpenAI Samples](https://github.com/Azure-Samples/openai)

### Related Projects
- [.NET Blazor Implementation](../dotnet/README.md) - Enterprise version with Aspire
- [Avatar SDK Documentation](https://learn.microsoft.com/azure/ai-services/speech-service/how-to-use-avatar)

## 🤝 Contributing

See the main [CONTRIBUTING](../CONTRIBUTING.md) guide for details.

## 📄 License

MIT License - see [LICENSE](../LICENSE) file for details.

---

**Need help?** Check the [main README](../README.md) or [open an issue](https://github.com/elbruno/customavatarlabs/issues)
