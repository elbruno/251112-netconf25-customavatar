# Azure AI Avatar Demo - AI Agent Instructions

## Project Overview

Browser-based talking avatar demo with two implementations: **JavaScript/HTML** (production-ready) and **.NET 9 Blazor** (modern alternative). Both integrate Azure Speech Services (STT/TTS + Avatar) and Azure OpenAI for interactive AI conversations.

## Repository Structure

```
├── python/                    # JavaScript implementation (primary)
│   ├── dev-server/           # Express.js local dev server (serves .env as /.env.json)
│   ├── js/                   # Core app logic (chat.js, config.js, theme-manager.js)
│   ├── css/                  # Styles (theme-system.css, theme-*.css)
│   ├── prompts/              # Prompt profiles (*.md templates + index.json)
│   └── .env                  # Azure credentials (gitignored)
├── *.html                    # Root HTML pages (chat.html, config.html, basic.html)
├── dotnet/                           # .NET 9 Blazor implementation
│   ├── AzureAIAvatarBlazor.AppHost/  # Aspire AppHost orchestration
│   ├── AzureAIAvatarBlazor.ServiceDefaults/  # Shared telemetry & resilience
│   └── AzureAIAvatarBlazor/          # Main Blazor app
│       ├── Components/Pages/         # Blazor pages (Chat.razor, Config.razor)
│       ├── Services/                 # Azure service wrappers (OpenAI, Speech, Configuration)
│       └── wwwroot/js/              # JavaScript interop (avatar-interop.js)
└── .vscode/tasks.json        # VS Code tasks for dev server
```

## Architecture

### JavaScript/HTML Implementation (python folder)

- **Static serving pattern**: Express serves `python/` folder FIRST (for js/css/images), then root (for HTML files)
- **Configuration flow**: `.env` file → `dev-server/server.js` → `/.env.json` endpoint → `js/config.js` → localStorage
- **Avatar connection**: `chat.js` uses Azure Speech SDK (loaded via CDN) → WebRTC peer connection → video rendering
- **Prompt system**: Markdown templates in `prompts/*.md` with `{{variable}}` placeholders, managed via `index.json`

### .NET Blazor Implementation

- **Orchestration**: .NET Aspire 9.5 (`AzureAIAvatarBlazor.AppHost`) manages the Blazor app lifecycle
- **Services**: Interface-based DI pattern (`IConfigurationService`, `IAzureSpeechService`, `IAzureOpenAIService`)
- **Configuration**: User Secrets (dev) → Environment Variables → appsettings.json (fallback)
- **Interop**: C# Blazor components ↔ `avatar-interop.js` ↔ Azure Speech SDK (browser)
- **Streaming**: OpenAI responses streamed via `IAsyncEnumerable<ChatStreamingUpdate>`
- **Telemetry**: OpenTelemetry via `ServiceDefaults` (metrics, traces, logs) → Aspire Dashboard
- **Resilience**: HTTP resilience policies via `Microsoft.Extensions.Http.Resilience`

## Critical Workflows

### Running the JavaScript App

```powershell
# Use VS Code Task (recommended - bypasses PowerShell ExecutionPolicy)
Tasks: Run Task → "Dev Server (HTTP)" or "Dev Server (HTTPS)"

# Or manually
cd python/dev-server
npm install
node server.js  # Serves on http://localhost:5173
```

**Key**: Server serves `python/` static assets BEFORE root HTML files. See `python/dev-server/server.js` lines 37-48.

### Running the .NET App

```bash
cd dotnet/AzureAIAvatarBlazor
dotnet user-secrets set "AzureSpeech:ApiKey" "your-key"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://..."
dotnet run  # Serves on https://localhost:5001
```

**Key**: Never edit `appsettings.Development.json` directly (gitignored). Use User Secrets or environment variables.

### Configuration Priority

1. **JavaScript**: ENV vars from `.env` → `/.env.json` → `js/config.js` → localStorage (browser)
2. **.NET**: User Secrets → ENV vars (AZURE*SPEECH*\*, SYSTEM_PROMPT) → appsettings.json → auto-detection

## Project-Specific Patterns

### Prompt Profile System

- Templates in `python/prompts/*.md` use Mustache-style `{{variables}}`
- `index.json` defines profiles with `id`, `name`, `file`, `defaults` (JSON object)
- Variable precedence: profile defaults < `PROMPT_VAR_*` env vars < UI JSON input
- Force profile on load: Set `PROMPT_ENFORCE_PROFILE=true` in `.env`

### Custom Avatar Handling

**Critical**: Custom avatars + built-in voice requires clearing `customVoiceEndpointId` to avoid silence/403 errors.

- JavaScript: `js/config.js` line ~200 (useBuiltInVoice logic)
- .NET: `Services/ConfigurationService.cs` auto-detects via `DetermineIfCustomAvatar()`

### Audio Gain Fix

Low avatar volume is a common issue. Solution:

- Stored in localStorage as `azureAIFoundryAudioGain` (default 1.8x)
- Applied via Web Audio API: `webAudioGainNode.gain.value = gainValue`
- JavaScript: `chat.js` creates `AudioContext` + `GainNode` in avatar connection flow
- .NET: Similar pattern in `avatar-interop.js`

### Environment Variable Mapping

The .NET app accepts BOTH naming conventions:

```
AZURE_SPEECH_API_KEY → AzureSpeech:ApiKey
SYSTEM_PROMPT → AzureOpenAI:SystemPrompt
TTS_VOICE → AzureSpeech:TtsVoice
AVATAR_CHARACTER → Avatar:Character
```

See `ConfigurationService.cs` for full mapping logic.

### Static File Serving Order (CRITICAL)

In `python/dev-server/server.js`, the order matters:

```javascript
app.use(express.static(pythonDir)); // Serve python/ FIRST
app.use(express.static(rootDir)); // Then root for HTML
```

This ensures `./js/chat.js` resolves to `python/js/chat.js`, not root (which doesn't exist).

## Common Issues

### "ENOENT: no such file or directory" when starting dev server

**Cause**: Server looking for files in wrong directory (e.g., `python/chat.html` instead of root)
**Fix**: Ensure `server.js` serves `python/` folder for assets, root for HTML files (see lines 37-48)

### Avatar silence or 403 errors

**Cause**: Mixing custom avatar with custom voice endpoint
**Fix**: Enable "Use Built-In Voice" checkbox OR clear `customVoiceEndpointId` field

### "No configuration found" in .NET app

**Cause**: Missing User Secrets or environment variables
**Fix**: Run `dotnet user-secrets list` to verify. Environment variables must use exact names (case-sensitive on Linux/macOS)

### HTTPS certificate errors in dev server

**Cause**: Missing or untrusted localhost certificates
**Fix**: Install `mkcert` and generate certs in `python/dev-server/certs/` (see README)

## Testing Checklist

When modifying code, verify:

1. ✅ Dev server starts without errors (`node server.js` in `python/dev-server/`)
2. ✅ `/.env.json` endpoint returns credentials (visit in browser)
3. ✅ `config.html` loads and saves to localStorage
4. ✅ Avatar session connects (check browser console for WebRTC errors)
5. ✅ Audio plays (check volume, Audio Gain setting)
6. ✅ Chat messages stream from Azure OpenAI
7. ✅ Prompt profiles load from `/api/prompts` endpoint

## Key Files to Understand

- `python/dev-server/server.js`: Dev server logic, `.env` exposure, prompt profile API
- `python/js/config.js`: Configuration management, avatar catalog, voice list
- `python/js/chat.js`: Avatar session lifecycle, WebRTC, audio processing
- `dotnet/AzureAIAvatarBlazor/Program.cs`: DI registration, middleware setup
- `dotnet/Services/ConfigurationService.cs`: Config priority logic, auto-detection
- `.vscode/tasks.json`: VS Code tasks for bypassing PowerShell restrictions

## Security Notes

- **Never commit** `.env`, `appsettings.Development.json`, or User Secrets
- Dev server (`server.js`) is **local development only** - exposes `.env` via HTTP
- Production deployment requires secure secret management (Azure Key Vault, Managed Identity)
- `.gitignore` already covers common credential files

## Extending the Project

- **Add new avatar**: Update `avatarCharacters` array in `js/config.js`
- **Add prompt profile**: Create `prompts/your-profile.md`, add entry to `prompts/index.json`
- **Add .NET service**: Follow interface pattern (`IYourService`), register in `Program.cs`
- **Modify UI theme**: Edit `python/css/theme-system.css` (CSS variables)
