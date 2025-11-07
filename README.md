## Azure AI Foundry TTS Talking Avatar Demo

Browser-based Talking Avatar built with Azure Speech (STT/TTS + Avatar) and Azure OpenAI. It ships with a Microsoft-inspired UI, persistent configuration, .env tooling for local dev, and a two‑column chat layout with subtitles and audio gain.

## Highlights
- Microsoft/Fluent‑style theme (Bootstrap 5 + Bootstrap Icons)
- Consistent header and full‑width gradient footer with developer credit
- Configuration UI with localStorage persistence and optional .env auto‑load (http/https only)
- Dependent dropdowns: Avatar Character → Style (custom avatars supported)
- TTS voice selector (popular voices + custom values)
- Built‑in voice routing for custom avatars (auto‑clears Custom Voice endpoint)
- Web Audio Gain (0.1–5.0x) for louder playback
- Chat by microphone and/or “Type Message” text input
- Subtitles and Auto‑Reconnect toggles
- Two‑column layout: Avatar (left) + Conversation (right)
- Quick “Test connection” buttons for Speech and Azure OpenAI

## Quick start
Option A — Open from disk (file://)
1) Open `config.html` or `chat.html` in your browser.
2) In `config.html`, click “Import .env (local file)” and select your `.env`. Values are applied and saved to localStorage.
3) Note: Auto‑loading `/.env.json` is intentionally skipped on file:// to avoid CORS noise.

Option B — Dev server for `.env.json`
1) Start the helper in `dev-server/`:

```powershell
cd dev-server
npm install
npm start
```

2) Serve the app over http/https (any static server) and open `config.html`.
3) The page will auto‑try `/.env.json` and populate values if available. This helper is for local development only.

PowerShell policy note (npm.ps1 blocked)
If you see "running scripts is disabled on this system" in PowerShell, you can start the server directly with Node and a temporary policy bypass for the current process:

Generic (adjust the path to your repo):
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass; cd "<path-to-repo>\dev-server"; node server.js
```

Example with this repo path:
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass; cd "c:\GIT Repos\AzureAIFoundry-TTS-Avatar-Demo\dev-server"; node server.js
```

### HTTPS on localhost (optional)
Some browsers/features require https. The dev server can start over HTTPS if local certs exist:

Default locations (already git‑ignored):
- `dev-server/certs/localhost-key.pem`
- `dev-server/certs/localhost.pem`

Generate trusted certs on Windows using mkcert:
1) Install mkcert (Chocolatey):
    ```powershell
    choco install mkcert -y
    ```
2) Install the local CA (one‑time):
    ```powershell
    mkcert -install
    ```
3) Create certs in `dev-server/certs`:
    ```powershell
    cd dev-server/certs
    mkcert -key-file localhost-key.pem -cert-file localhost.pem localhost 127.0.0.1 ::1
    ```
4) Start the server (uses HTTPS automatically if certs are present):
    ```powershell
    cd ../
    npm start
    # o: node server.js
    ```

Overrides (opcional):
```powershell
$env:SSL_KEY_PATH="C:\ruta\a\mi-key.pem"
$env:SSL_CERT_PATH="C:\ruta\a\mi-cert.pem"
npm start
```

Ahora podrás abrir:
- https://localhost:5173/
- https://localhost:5173/config.html
- Endpoint: https://localhost:5173/.env.json

## Configuration guide (config.html)
Azure Speech
- Region, API Key, optional Private Endpoint
- Use “Test Speech Connection” to verify credentials quickly

Azure OpenAI
- Endpoint, API Key, Deployment Name, System Prompt
- Use “Test OpenAI Connection” for a minimal chat probe
 - Optional: Prompt Profiles (dropdown) loaded from `/prompts/index.json` and variable interpolation

On Your Data (optional)
- Azure AI Search Endpoint, API Key, Index Name
- Enabled only when the toggle is checked

STT/TTS
- STT Locales list (e.g., `en-US,de-DE,…`) for auto language detection
- TTS Voice dropdown with common voices; custom names from .env/URL are supported
- Custom Voice Deployment ID (Endpoint ID) for your Custom Voice
- Audio Gain (0.1–5.0x), stored as `azureAIFoundryAudioGain` in localStorage

Avatar
- Character dropdown auto‑filters available Styles
- Custom Avatar unlocks “Use Built‑In Voice”; when on, the Custom Voice endpoint is cleared to avoid conflicts/silence
- Some custom avatars require an empty Style value (e.g., special cases)
- Subtitles and Auto‑Reconnect toggles

## Using the Chat (chat.html)
1) Ensure Speech + Azure OpenAI settings are configured (via `config.html` or localStorage).
2) Click “Open Avatar Session”.
3) Use the microphone (Start Microphone) or enable “Type Message” to send text.
4) Layout:
     - Left (lg‑8): avatar video (responsive)
     - Right (lg‑4): conversation history (scrollable) + input box

Tips
- Volume too low? Raise Audio Gain in `config.html`; it applies on the next audio connection.
- Custom avatars with built‑in voice: enable “Custom Avatar”, then check “Use Built‑In Voice”. The app routes TTS correctly and clears any Custom Voice endpoint.
- The footer includes developer credit and links (LinkedIn + GitHub).

## Environment variables (.env)
The UI can import a local `.env` file, or a dev server can expose `/.env.json`. Supported keys map directly to config fields:

```
# Azure Speech
AZURE_SPEECH_API_KEY=your_speech_key
AZURE_SPEECH_REGION=westus2
AZURE_SPEECH_PRIVATE_ENDPOINT=https://your-name.cognitiveservices.azure.com/

# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
AZURE_OPENAI_API_KEY=your_openai_key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini
SYSTEM_PROMPT=You are an AI assistant that helps people find information.
PROMPT_PROFILE=pablito-piova-es
# Force the selected profile to be applied on every load
PROMPT_ENFORCE_PROFILE=true
# Variables for prompt templates (keys after PROMPT_VAR_ are lowercased for interpolation)
PROMPT_VAR_tone=helpful
PROMPT_VAR_format=concise


### Ejecutar con un clic desde VS Code
Se añadieron tareas de VS Code para arrancar el servidor sin pelear con la política de PowerShell.

1) Abre la paleta de comandos y ejecuta: “Tasks: Run Task”.
2) Elige una de:
    - Dev Server (HTTP)
    - Dev Server (HTTPS)

Ambas usan `-ExecutionPolicy Bypass` solo para esa terminal de VS Code y ejecutan `node server.js` en `dev-server/`.
# Azure AI Search (optional)
AZURE_COGNITIVE_SEARCH_ENDPOINT=https://your-search.search.windows.net
AZURE_COGNITIVE_SEARCH_API_KEY=your_search_key
AZURE_COGNITIVE_SEARCH_INDEX_NAME=your_index

# TTS / Avatar
TTS_VOICE=en-US-AvaMultilingualNeural
CUSTOM_VOICE_ENDPOINT_ID=
AVATAR_CHARACTER=lisa
AVATAR_STYLE=casual-sitting
ENABLE_CONTINUOUS_CONVERSATION=false
ENABLE_SUBTITLES=false
ENABLE_AUTO_RECONNECT=false
```

Notes
- Boolean values accept `true`/`false` (case‑insensitive).
- Secrets are never hardcoded; the dev server is local‑only and not for production.
 - Prompt Profiles: Create templates in `/prompts/*.md` and list them in `/prompts/index.json`. Use `{{var}}` placeholders.
 - Variables precedence: defaults from profile < PROMPT_VAR_* from .env < JSON in the “Prompt Variables” box.
 - To always use one profile (e.g., “Pablito Piova (ES)”), set `PROMPT_PROFILE=pablito-piova-es` and `PROMPT_ENFORCE_PROFILE=true`. This overrides any saved System Prompt.

## Project structure
```
├── basic.html                # Legacy demo (disabled banner shown)
├── chat.html                 # Chat experience (two‑column layout)
├── config.html               # Configuration UI
├── css/
│   ├── styles.css
│   └── theme-microsoft.css   # Microsoft/Fluent‑inspired theme
├── js/
│   ├── basic.js
│   ├── chat.js
│   └── config.js
├── dev-server/
│   ├── package.json
│   └── server.js             # local‑only .env → /.env.json helper
├── image/ | avatar/ | video/
├── .env.example
└── README.md
```

## Troubleshooting
- 401/403 or silence
    - Check keys/region and avoid mixing Built‑In Voice with a Custom Voice endpoint
- Low volume
    - Increase Audio Gain in `config.html` (0.1–5.0x)
- 4429 throttling / concurrency
    - Don’t start multiple sessions at once; the app tears down old sessions before starting a new one. If it persists, wait ~30 seconds and retry
- 1007 “not owned” (custom avatar)
    - Ensure the avatar exists in your Speech resource/region and is owned by your account
- CORS on file://
    - Expected for `/.env.json`; use the import button or run the dev helper over http
- Autoplay policies
    - Some browsers require a user gesture before audio; if silent, click the page or press a button to resume the audio context

## Customization
- Theme/colors: edit `css/theme-microsoft.css` (CSS variables + navbar/footer gradients)
- Voices/avatars: extend lists in `js/config.js` (voice dropdown + avatar catalog)
- Layout: tweak Bootstrap columns in `chat.html` (e.g., `col-lg-8` / `col-lg-4`)

## Security
- Don’t commit secrets. `.env` is git‑ignored.
- Rotate any secret that might have leaked.

## License
MIT
