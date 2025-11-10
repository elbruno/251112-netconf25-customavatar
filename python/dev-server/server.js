// Simple dev server to expose .env as JSON for the browser
// Do NOT use in production. This is for local development only.

const express = require('express');
const path = require('path');
const fs = require('fs');
const http = require('http');
const https = require('https');
const dotenv = require('dotenv');

const app = express();
const port = Number(process.env.PORT) || 5173;

// Load .env one level up (repo root) by default
const envPath = process.env.ENV_PATH || path.resolve(__dirname, '..', '.env');
let env = {};
if (fs.existsSync(envPath)) {
  const parsed = dotenv.parse(fs.readFileSync(envPath));
  env = parsed;
  console.log('[dev-server] Loaded .env from', envPath);
} else {
  console.warn('[dev-server] No .env found at', envPath);
}

// Allow CORS for local file use via Simple Browser or direct file://
app.use((req, res, next) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET,POST,PUT,DELETE,OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
  if (req.method === 'OPTIONS') {
    res.status(200).end();
    return;
  }
  next();
});

// Parse JSON bodies for simple local APIs (dev only)
app.use(express.json({ limit: '1mb' }));

// Serve static assets from the python folder FIRST (js, css, prompts, image, avatar)
const pythonDir = path.resolve(__dirname, '..');
app.use(express.static(pythonDir));

// Also serve from repository root for HTML files
const rootDir = path.resolve(__dirname, '..', '..');
app.use(express.static(rootDir));

// Default route -> chat.html (from root)
app.get('/', (req, res) => {
  res.sendFile(path.join(rootDir, 'chat.html'));
});

// Serve env vars as JSON
app.get('/.env.json', (req, res) => {
  // Whitelist only variables we expect to be used by the client
  const allowedKeys = [
    'AZURE_SPEECH_API_KEY',
    'AZURE_SPEECH_REGION',
    'AZURE_SPEECH_PRIVATE_ENDPOINT',
    'AZURE_OPENAI_ENDPOINT',
    'AZURE_OPENAI_API_KEY',
    'AZURE_OPENAI_DEPLOYMENT_NAME',
  // System prompt for chat UI (with common aliases)
  'SYSTEM_PROMPT',
  'OPENAI_SYSTEM_PROMPT',
  'PROMPT',
  'PROMPT_PROFILE',
  // Force applying the selected profile on load
  'PROMPT_ENFORCE_PROFILE',
  'FORCE_PROMPT_PROFILE',
  'PROMPT_PROFILE_FORCE',
    'AZURE_COGNITIVE_SEARCH_ENDPOINT',
    'AZURE_COGNITIVE_SEARCH_API_KEY',
    'AZURE_COGNITIVE_SEARCH_INDEX_NAME',
    'TTS_VOICE',
    'CUSTOM_VOICE_ENDPOINT_ID',
    'AVATAR_CHARACTER',
    'AVATAR_STYLE',
    'ENABLE_CONTINUOUS_CONVERSATION',
    'ENABLE_SUBTITLES',
    'ENABLE_AUTO_RECONNECT'
  ];

  const payload = {};
  for (const key of allowedKeys) {
    if (env[key] !== undefined) payload[key] = env[key];
  }
  // Also expose prompt variables following PROMPT_VAR_* convention
  for (const [k, v] of Object.entries(env)) {
    if (k.startsWith('PROMPT_VAR_')) {
      payload[k] = v;
    }
  }
  // Prevent caching during local development
  res.setHeader('Cache-Control', 'no-store');
  res.json(payload);
});

// ----- Local-only APIs for managing prompt profiles (DO NOT USE IN PROD) -----
const promptsDir = path.resolve(__dirname, '..', 'prompts');
const promptsIndexPath = path.join(promptsDir, 'index.json');

function readPromptIndex() {
  try {
    const raw = fs.readFileSync(promptsIndexPath, 'utf8');
    return JSON.parse(raw);
  } catch (e) {
    return { profiles: [] };
  }
}

function writePromptIndex(data) {
  const text = JSON.stringify(data, null, 2);
  fs.writeFileSync(promptsIndexPath, text, 'utf8');
}

app.get('/api/prompts', (req, res) => {
  try {
    const idx = readPromptIndex();
    res.json(idx);
  } catch (e) {
    res.status(500).json({ error: e.message });
  }
});

app.get('/api/prompts/:id', (req, res) => {
  try {
    const id = req.params.id;
    const idx = readPromptIndex();
    const p = (idx.profiles || []).find(x => x.id === id);
    if (!p) return res.status(404).json({ error: 'Not found' });
    const filePath = path.join(promptsDir, p.file || (id + '.md'));
    let content = '';
    if (fs.existsSync(filePath)) content = fs.readFileSync(filePath, 'utf8');
    res.json({ profile: p, content });
  } catch (e) {
    res.status(500).json({ error: e.message });
  }
});

// Upsert profile and template content
app.post('/api/prompts/upsert', (req, res) => {
  try {
    const { profile, content } = req.body || {};
    if (!profile || !profile.id) return res.status(400).json({ error: 'profile.id is required' });
    const idx = readPromptIndex();
    let list = Array.isArray(idx.profiles) ? idx.profiles : [];
    let existing = list.find(x => x.id === profile.id);
    if (existing) {
      Object.assign(existing, profile);
    } else {
      list.push(profile);
    }
    idx.profiles = list;
    writePromptIndex(idx);
    // Write template file if provided
    if (typeof content === 'string') {
      const fileName = profile.file || (profile.id + '.md');
      const filePath = path.join(promptsDir, fileName);
      fs.writeFileSync(filePath, content, 'utf8');
    }
    res.json({ ok: true });
  } catch (e) {
    res.status(500).json({ error: e.message });
  }
});

// Delete profile (and optionally its file if matches id.md)
app.delete('/api/prompts/:id', (req, res) => {
  try {
    const id = req.params.id;
    const idx = readPromptIndex();
    const list = Array.isArray(idx.profiles) ? idx.profiles : [];
    const p = list.find(x => x.id === id);
    if (!p) return res.status(404).json({ error: 'Not found' });
    idx.profiles = list.filter(x => x.id !== id);
    writePromptIndex(idx);
    // Best-effort delete if filename is default pattern
    const defaultFile = id + '.md';
    const fileToCheck = p.file || defaultFile;
    if (fileToCheck === defaultFile) {
      const filePath = path.join(promptsDir, fileToCheck);
      if (fs.existsSync(filePath)) fs.unlinkSync(filePath);
    }
    res.json({ ok: true });
  } catch (e) {
    res.status(500).json({ error: e.message });
  }
});

// Optional HTTPS support for local dev
// It will enable HTTPS if both key and cert files exist.
// Default paths: dev-server/certs/localhost-key.pem and dev-server/certs/localhost.pem
// You can override with env: SSL_KEY_PATH and SSL_CERT_PATH
const certsDir = path.resolve(__dirname, 'certs');
const keyPath = process.env.SSL_KEY_PATH || path.join(certsDir, 'localhost-key.pem');
const certPath = process.env.SSL_CERT_PATH || path.join(certsDir, 'localhost.pem');

let server;
if (fs.existsSync(keyPath) && fs.existsSync(certPath)) {
  try {
    const key = fs.readFileSync(keyPath);
    const cert = fs.readFileSync(certPath);
    server = https.createServer({ key, cert }, app);
    server.listen(port, () => {
      console.log(`[dev-server] HTTPS enabled`);
      console.log(`[dev-server] Listening on https://localhost:${port}`);
      console.log('[dev-server] Endpoint: https://localhost:' + port + '/.env.json');
    });
  } catch (e) {
    console.warn('[dev-server] Failed to start HTTPS, falling back to HTTP. Reason:', e.message);
  }
}

if (!server) {
  server = http.createServer(app);
  server.listen(port, () => {
    console.log(`[dev-server] Listening on http://localhost:${port}`);
    console.log('[dev-server] Endpoint: http://localhost:' + port + '/.env.json');
  });
}
