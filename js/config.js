// Configuration Management for Azure AI Foundry TTS Avatar Demo
// Handles configuration loading, saving, and environment variables

// Default configuration values
const DEFAULT_CONFIG = {
    // Azure Speech Resource
    region: 'westus2',
    APIKey: '',
    enablePrivateEndpoint: false,
    privateEndpoint: '',
    
    // Azure OpenAI Resource
    azureOpenAIEndpoint: '',
    azureOpenAIApiKey: '',
    azureOpenAIDeploymentName: '',
    prompt: 'You are an AI assistant that helps people find information.',
    // Prompt profiles and variables
    promptProfile: '',
    // When true, always apply the selected prompt profile on load, even if prompt isn't default
    promptProfileForce: false,
    promptVars: '', // JSON string of variables for interpolation
    enableOyd: false,
    
    // Azure Cognitive Search Resource
    azureCogSearchEndpoint: '',
    azureCogSearchApiKey: '',
    azureCogSearchIndexName: '',
    
    // STT / TTS Configuration
    sttLocales: 'en-US,de-DE,es-ES,fr-FR,it-IT,ja-JP,ko-KR,zh-CN',
    ttsVoice: 'en-US-AvaMultilingualNeural',
    customVoiceEndpointId: '',
    continuousConversation: false,
    
    // Avatar Configuration
    talkingAvatarCharacter: 'pablitopiovaagentcon',
    talkingAvatarStyle: '',
    customizedAvatar: true,
    useBuiltInVoice: true,
    autoReconnectAvatar: false,
    useLocalVideoForIdle: false,
    showSubtitles: false
};

// Environment variable mapping
const ENV_MAPPING = {
    'AZURE_SPEECH_API_KEY': 'APIKey',
    'AZURE_SPEECH_REGION': 'region',
    'AZURE_SPEECH_PRIVATE_ENDPOINT': 'privateEndpoint',
    'AZURE_OPENAI_ENDPOINT': 'azureOpenAIEndpoint',
    'AZURE_OPENAI_API_KEY': 'azureOpenAIApiKey',
    'AZURE_OPENAI_DEPLOYMENT_NAME': 'azureOpenAIDeploymentName',
    // System prompt for chat
    'SYSTEM_PROMPT': 'prompt',
    'OPENAI_SYSTEM_PROMPT': 'prompt',
    'PROMPT': 'prompt',
    // Optional: choose a profile from /prompts/index.json
    'PROMPT_PROFILE': 'promptProfile',
    // Force applying profile regardless of current prompt
    'PROMPT_ENFORCE_PROFILE': 'promptProfileForce',
    'FORCE_PROMPT_PROFILE': 'promptProfileForce',
    'PROMPT_PROFILE_FORCE': 'promptProfileForce',
    'AZURE_COGNITIVE_SEARCH_ENDPOINT': 'azureCogSearchEndpoint',
    'AZURE_COGNITIVE_SEARCH_API_KEY': 'azureCogSearchApiKey',
    'AZURE_COGNITIVE_SEARCH_INDEX_NAME': 'azureCogSearchIndexName',
    'TTS_VOICE': 'ttsVoice',
    'CUSTOM_VOICE_ENDPOINT_ID': 'customVoiceEndpointId',
    'AVATAR_CHARACTER': 'talkingAvatarCharacter',
    'AVATAR_STYLE': 'talkingAvatarStyle',
    'ENABLE_CONTINUOUS_CONVERSATION': 'continuousConversation',
    'ENABLE_SUBTITLES': 'showSubtitles',
    'ENABLE_AUTO_RECONNECT': 'autoReconnectAvatar'
};

class ConfigurationManager {
    constructor() {
        this.config = { ...DEFAULT_CONFIG };
        this.envLoaded = false;
        this.promptProfiles = [];
        // Helper to identify placeholder/invalid endpoint IDs
        this.isPlaceholderEndpointId = (val) => {
            const v = String(val || '').trim().toLowerCase();
            if (!v) return true; // empty is considered non-custom
            if (v === 'your_custom_voice_endpoint_id') return true;
            if (v.startsWith('xxxxx')) return true;
            return false;
        };
        // Mapa de personajes y estilos disponibles
        this.avatarCatalog = {
            harry: ['business', 'casual', 'youthful'],
            jeff: ['business', 'formal'],
            lisa: ['casual-sitting'],
            lori: ['casual', 'formal', 'graceful'],
            max: ['business', 'casual', 'formal'],
            meg: ['business', 'casual', 'formal'],
            pablitopiova: [''], // sin style especificado
            pablitopiovaagentcon: [''] // nuevo custom avatar, estilo vacío
        };
        // Voces TTS disponibles en el dropdown
        this.ttsVoices = [
            'en-US-AvaMultilingualNeural',
            'en-US-AndrewMultilingualNeural',
            'en-US-AdamMultilingualNeural',
            'en-US-AmandaMultilingualNeural'
        ];
        this.initializeConfiguration();
    }

    // Initialize configuration from various sources
    initializeConfiguration() {
        // Load from environment variables first (simulated .env)
        this.loadFromEnvironment();
        
        // Then load from localStorage (overrides environment)
        this.loadFromLocalStorage();
        
        // Finally load from URL parameters (overrides everything)
        this.loadFromURLParams();
        
        // Apply configuration to UI when page loads
    if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
        this.setupAvatarDropdowns();
                this.setupTtsVoiceDropdown();
                this.setupPromptProfiles();
                this.applyConfigurationToUI();
        this.syncAvatarDropdownsFromConfig();
                this.syncTtsVoiceFromConfig();
                this.showEnvironmentStatus();
                // Apply profile: if force flag is on OR when prompt is still default
                if (this.config.promptProfile && (this.config.promptProfileForce || this.config.prompt === DEFAULT_CONFIG.prompt)) {
                    this.applyPromptProfileIfConfigured().catch(()=>{});
                }
                // Non-blocking: attempt to auto-load dev .env if a server is running
                this.autoTryLoadDevEnvIfAvailable();
            });
        } else {
        this.setupAvatarDropdowns();
            this.setupTtsVoiceDropdown();
            this.setupPromptProfiles();
            this.applyConfigurationToUI();
        this.syncAvatarDropdownsFromConfig();
            this.syncTtsVoiceFromConfig();
            this.showEnvironmentStatus();
            if (this.config.promptProfile && (this.config.promptProfileForce || this.config.prompt === DEFAULT_CONFIG.prompt)) {
                this.applyPromptProfileIfConfigured().catch(()=>{});
            }
            // Non-blocking: attempt to auto-load dev .env if a server is running
            this.autoTryLoadDevEnvIfAvailable();
        }
    }

    // Show status of environment variable loading
    showEnvironmentStatus() {
        if (this.envLoaded) {
            this.showMessage('✅ Configuration loaded from dev .env.', 'success');
        }
    }

    // Try to load .env from dev server automatically and persist
    async autoTryLoadDevEnvIfAvailable() {
        try {
            // Solo intentar en orígenes http/https; evitar file:// para no generar errores CORS en local
            const proto = (typeof location !== 'undefined' ? location.protocol : '')
            if (!proto || (proto !== 'http:' && proto !== 'https:')) {
                return
            }
            const envValues = await this.tryLoadRealEnvFile();
            if (!envValues) return; // silently skip if not available
            this.config = { ...DEFAULT_CONFIG };
            this.applyEnvValues(envValues);
            // Apply profile based on force flag or default prompt condition
            try {
                if (this.config.promptProfile && (this.config.promptProfileForce || this.config.prompt === DEFAULT_CONFIG.prompt)) {
                    await this.applyPromptProfileIfConfigured();
                }
            } catch {}
            this.applyConfigurationToUI();
            try {
                localStorage.setItem('azureAIFoundryConfig', JSON.stringify(this.config));
            } catch {}
            this.envLoaded = true;
            this.showEnvironmentStatus();
        } catch {}
    }

    // Método 2: Función para intentar leer el archivo .env real (solo en desarrollo)
    async tryLoadRealEnvFile() {
        try {
            // NOTA: En desarrollo, podrías usar un servidor local para servir el .env
            // Por ejemplo, un simple servidor Node.js que lea el .env y lo sirva como JSON
            
            // Intentar cargar desde un endpoint local explícito (si existe)
            let response = await fetch('http://localhost:5173/.env.json', { cache: 'no-store' }).catch(() => null);
            // Fallback a ruta relativa si se está sirviendo desde el mismo origen
            if (!response || !response.ok) {
                response = await fetch('/.env.json', { cache: 'no-store' }).catch(() => null);
            }
            if (response && response.ok) {
                const envData = await response.json();
                console.log('Loaded real .env file via local server');
                this.envLoaded = true;
                return envData;
            }
        } catch (error) {
            console.log('Could not load real .env file:', error.message);
        }
        
        // Fallback a valores simulados
        return null;
    }

    // Load configuration from environment variables
    loadFromEnvironment() {
        // In a browser environment, we simulate environment variables
        // In a real deployment, these would come from your server or build process
    const envVars = this.getEnvironmentVariables();
        
        for (const [envVar, configKey] of Object.entries(ENV_MAPPING)) {
            if (envVars[envVar] !== undefined && envVars[envVar] !== '') {
                let value = envVars[envVar];
                
                // Handle boolean values
                if (typeof value === 'string') {
                    if (value.toLowerCase() === 'true') {
                        value = true;
                    } else if (value.toLowerCase() === 'false') {
                        value = false;
                    }
                }
                
                this.config[configKey] = value;
                console.log(`Loaded ${envVar} -> ${configKey}:`, value);
            }
        }
    }

    // Simulate environment variables (in production, these would be injected)
    getEnvironmentVariables() {
        // No hardcoded secrets. Real values should be served by a local dev server (/.env.json).
        return {};
    }

    // Load configuration from localStorage
    loadFromLocalStorage() {
        try {
            const savedConfig = localStorage.getItem('azureAIFoundryConfig');
            if (savedConfig) {
                const parsedConfig = JSON.parse(savedConfig);
                this.config = { ...this.config, ...parsedConfig };
            }
        } catch (error) {
            console.warn('Failed to load configuration from localStorage:', error);
        }
    }

    // Load configuration from URL parameters
    loadFromURLParams() {
        const urlParams = new URLSearchParams(window.location.search);
        
        // Map URL parameters to configuration keys
        const urlMappings = {
            'region': 'region',
            'endpoint': 'azureOpenAIEndpoint',
            'deployment': 'azureOpenAIDeploymentName',
            'voice': 'ttsVoice',
            'character': 'talkingAvatarCharacter',
            'style': 'talkingAvatarStyle'
        };

        for (const [urlParam, configKey] of Object.entries(urlMappings)) {
            const value = urlParams.get(urlParam);
            if (value) {
                this.config[configKey] = value;
            }
        }
    }

    // Apply configuration to UI elements
    applyConfigurationToUI() {
        for (const [key, value] of Object.entries(this.config)) {
            const element = document.getElementById(key);
            if (element) {
                if (element.type === 'checkbox') {
                    element.checked = value;
                } else if (element.tagName === 'SELECT') {
                    // Para selects, si el valor no existe, lo dejamos sin cambios
                    const hasOption = Array.from(element.options).some(o => o.value === String(value));
                    if (hasOption) element.value = String(value);
                } else {
                    element.value = value;
                }
            }
        }
        // If avatar is custom and endpoint id is empty/placeholder → allow built-in voice by default
        try {
            const isCustom = !!this.config.customizedAvatar;
            const endpointId = this.config.customVoiceEndpointId;
            if (isCustom && this.isPlaceholderEndpointId(endpointId)) {
                this.config.customVoiceEndpointId = '';
                this.config.useBuiltInVoice = true;
            }
        } catch {}
        // Sincronizar estado de Use Built-In Voice según Custom Avatar
        if (typeof window.updateCustomAvatarBox === 'function') {
            window.updateCustomAvatarBox();
        }
    }

    // Save current configuration to localStorage
    saveConfiguration() {
        this.updateConfigFromUI();
        try {
            localStorage.setItem('azureAIFoundryConfig', JSON.stringify(this.config));
            this.showMessage('Configuration saved successfully!', 'success');
        } catch (error) {
            console.error('Failed to save configuration:', error);
            this.showMessage('Failed to save configuration: ' + error.message, 'error');
        }
    }

    // Update configuration object from UI values
    updateConfigFromUI() {
        for (const key of Object.keys(this.config)) {
            const element = document.getElementById(key);
            if (element) {
                if (element.type === 'checkbox') {
                    this.config[key] = element.checked;
                } else {
                    this.config[key] = element.value;
                }
            }
        }
    }

    // Load configuration and apply to UI
    loadConfiguration() {
        this.loadFromLocalStorage();
        this.applyConfigurationToUI();
        this.showMessage('Configuration loaded successfully!', 'success');
    }

    // Parse .env text content into a key/value object
    parseDotEnvText(text) {
        const out = {};
        if (!text) return out;
        const lines = text.split(/\r?\n/);
        for (let raw of lines) {
            const line = raw.trim();
            if (!line || line.startsWith('#')) continue;
            const eq = line.indexOf('=');
            if (eq === -1) continue;
            const key = line.substring(0, eq).trim();
            let val = line.substring(eq + 1).trim();
            if ((val.startsWith('"') && val.endsWith('"')) || (val.startsWith("'") && val.endsWith("'"))) {
                val = val.substring(1, val.length - 1);
            }
            out[key] = val;
        }
        return out;
    }

    // Apply env key/value pairs to config using ENV_MAPPING
    applyEnvValues(envValues) {
        for (const [envVar, configKey] of Object.entries(ENV_MAPPING)) {
            if (envValues[envVar] !== undefined && envValues[envVar] !== '') {
                let value = envValues[envVar];
                if (typeof value === 'string') {
                    const lower = value.toLowerCase();
                    if (lower === 'true') value = true;
                    else if (lower === 'false') value = false;
                }
                // Sanitize placeholder endpoint ids
                if (configKey === 'customVoiceEndpointId' && this.isPlaceholderEndpointId(value)) {
                    value = '';
                }
                this.config[configKey] = value;
            }
        }
        // Collect PROMPT_VAR_* into promptVars JSON
        const vars = {};
        for (const [k, v] of Object.entries(envValues || {})) {
            if (k.startsWith('PROMPT_VAR_')) {
                const key = k.substring('PROMPT_VAR_'.length).toLowerCase();
                vars[key] = v;
            }
        }
        if (Object.keys(vars).length) {
            try { this.config.promptVars = JSON.stringify(vars); } catch {}
        }
    }

    // Import a local .env file (client-side only, no network)
    importDotEnvFile() {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.env,.txt';
        input.onchange = (event) => {
            const file = event.target.files && event.target.files[0];
            if (!file) return;
            const reader = new FileReader();
            reader.onload = (e) => {
                try {
                    const envObj = this.parseDotEnvText(String(e.target.result || ''));
                    this.config = { ...DEFAULT_CONFIG };
                    this.applyEnvValues(envObj);
                    // If PROMPT_PROFILE is present and force flag or prompt is default, auto-apply
                    const shouldApply = !!this.config.promptProfile && (this.config.promptProfileForce || this.config.prompt === DEFAULT_CONFIG.prompt);
                    this.applyConfigurationToUI();
                    if (shouldApply) {
                        this.applyPromptProfileIfConfigured().catch(()=>{}).then(() => this.applyConfigurationToUI());
                    }
                    try {
                        localStorage.setItem('azureAIFoundryConfig', JSON.stringify(this.config));
                    } catch {}
                    this.showMessage('Configuration loaded from .env and saved.', 'success');
                } catch (err) {
                    console.error('Failed to parse .env:', err);
                    this.showMessage('Failed to parse .env file', 'error');
                }
            };
            reader.readAsText(file);
        };
        input.click();
    }

    // Export configuration as JSON file
    exportConfiguration() {
        this.updateConfigFromUI();
        const configJSON = JSON.stringify(this.config, null, 2);
        const blob = new Blob([configJSON], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        
        const a = document.createElement('a');
        a.href = url;
        a.download = 'azure-ai-foundry-config.json';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        
        this.showMessage('Configuration exported successfully!', 'success');
    }

    // Import configuration from JSON file
    importConfiguration() {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.json';
        
        input.onchange = (event) => {
            const file = event.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    try {
                        const importedConfig = JSON.parse(e.target.result);
                        this.config = { ...DEFAULT_CONFIG, ...importedConfig };
                        this.applyConfigurationToUI();
                        this.showMessage('Configuration imported successfully!', 'success');
                    } catch (error) {
                        console.error('Failed to import configuration:', error);
                        this.showMessage('Failed to import configuration: Invalid JSON file', 'error');
                    }
                };
                reader.readAsText(file);
            }
        };
        
        input.click();
    }

    // Reset configuration to defaults
    resetConfiguration() {
        if (confirm('Are you sure you want to reset all configuration to defaults?')) {
            this.config = { ...DEFAULT_CONFIG };
            this.applyConfigurationToUI();
            localStorage.removeItem('azureAIFoundryConfig');
            this.showMessage('Configuration reset to defaults!', 'success');
        }
    }

    // Función especial para cargar desde archivo .env (Método 2)
    async loadFromEnvFile() {
        console.log('Loading configuration from dev .env via local server...');
        const envValues = await this.tryLoadRealEnvFile();
        if (!envValues) {
            this.showMessage('No dev .env server detected. Nothing loaded.', 'info');
            return;
        }

        // Limpiar configuración actual y aplicar valores mapeados
        this.config = { ...DEFAULT_CONFIG };
        for (const [envVar, configKey] of Object.entries(ENV_MAPPING)) {
            if (envValues[envVar] !== undefined && envValues[envVar] !== '') {
                let value = envValues[envVar];
                if (typeof value === 'string') {
                    const lower = value.toLowerCase();
                    if (lower === 'true') value = true;
                    else if (lower === 'false') value = false;
                }
                this.config[configKey] = value;
            }
        }
        const shouldApply = !!this.config.promptProfile && (this.config.promptProfileForce || this.config.prompt === DEFAULT_CONFIG.prompt);
        this.applyConfigurationToUI();
        if (shouldApply) {
            try { await this.applyPromptProfileIfConfigured(); } catch {}
            this.applyConfigurationToUI();
        }
        this.showMessage('Configuration loaded from dev .env!', 'success');
    }

    // Get current configuration
    getConfiguration() {
        this.updateConfigFromUI();
        return { ...this.config };
    }

    // Show message to user
    showMessage(message, type = 'info') {
        // Prefer SweetAlert2 when available
        if (window.Swal && typeof window.Swal.fire === 'function') {
            const iconMap = { success: 'success', error: 'error', info: 'info', warning: 'warning' };
            window.Swal.fire({
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 2500,
                timerProgressBar: true,
                icon: iconMap[type] || 'info',
                title: message
            });
            return;
        }
        // Fallback: simple inline toast
        const existing = document.getElementById('configMessage');
        if (existing) existing.remove();
        const div = document.createElement('div');
        div.id = 'configMessage';
        div.textContent = message;
        div.style.cssText = `position:fixed;top:20px;right:20px;padding:10px 20px;border-radius:5px;color:#fff;font-weight:bold;z-index:1000;background-color:${type === 'success' ? '#4CAF50' : type === 'error' ? '#f44336' : '#2196F3'}`;
        document.body.appendChild(div);
        setTimeout(() => div.remove(), 3000);
    }

    // Setup audio gain control slider
    setupAudioGainControl() {
        const nativeSlider = document.getElementById('audioGainSlider');
        const label = document.getElementById('audioGainValue');
        const container = document.getElementById('audioGainSliderContainer');
        if (!label) return;
        let current = Number(localStorage.getItem('azureAIFoundryAudioGain') || '1.8');
        if (!isFinite(current) || current < 0.1) current = 1.8;
        if (current > 5.0) current = 5.0;
        const setValue = (v) => {
            const clamped = Math.max(0.1, Math.min(5.0, Number(v)));
            label.textContent = `${clamped.toFixed(1)}x`;
            try { localStorage.setItem('azureAIFoundryAudioGain', String(clamped)); } catch {}
        };
        // Try noUiSlider
        if (container && window.noUiSlider) {
            try {
                container.innerHTML = '';
                window.noUiSlider.create(container, {
                    start: [current],
                    connect: [true, false],
                    range: { min: 0.1, max: 5.0 },
                    step: 0.1,
                    tooltips: { to: (v) => `${Number(v).toFixed(1)}x` }
                });
                setValue(current);
                if (nativeSlider) nativeSlider.style.display = 'none';
                container.noUiSlider.on('update', (values) => setValue(values[0]));
                return;
            } catch {}
        }
        // Fallback: native range input
        if (nativeSlider) {
            nativeSlider.value = String(current);
            setValue(current);
            nativeSlider.oninput = () => setValue(nativeSlider.value);
        }
    }
}

// Patch initializeConfiguration to call setupAudioGainControl after DOM load
(function() {
  const origInit = ConfigurationManager.prototype.initializeConfiguration;
  ConfigurationManager.prototype.initializeConfiguration = function() {
    origInit.apply(this, arguments);
    const ready = () => this.setupAudioGainControl();
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', ready);
    } else {
      ready();
    }
  };
})();

// Global configuration manager instance
window.configManager = new ConfigurationManager();

// Expose functions to global scope for HTML onclick handlers
window.loadFromEnvFile = () => window.configManager.loadFromEnvFile();
window.saveConfiguration = () => window.configManager.saveConfiguration();
window.loadConfiguration = () => window.configManager.loadConfiguration();
window.exportConfiguration = () => window.configManager.exportConfiguration();
window.importConfiguration = () => window.configManager.importConfiguration();
window.resetConfiguration = () => window.configManager.resetConfiguration();
window.importDotEnvFile = () => window.configManager.importDotEnvFile();
window.applyPromptProfile = () => window.configManager.applyPromptProfile();

// Update functions for checkboxes
window.updatePrivateEndpoint = () => {
    const enablePrivateEndpoint = document.getElementById('enablePrivateEndpoint');
    const privateEndpointDiv = document.getElementById('showPrivateEndpointCheckBox');
    
    if (enablePrivateEndpoint.checked) {
        privateEndpointDiv.removeAttribute('hidden');
    } else {
        privateEndpointDiv.setAttribute('hidden', 'hidden');
    }
};

window.updateEnableOyd = () => {
    const enableOyd = document.getElementById('enableOyd');
    const cogSearchConfig = document.getElementById('cogSearchConfig');
    
    if (enableOyd.checked) {
        cogSearchConfig.removeAttribute('hidden');
    } else {
        cogSearchConfig.setAttribute('hidden', 'hidden');
    }
};

window.updateCustomAvatarBox = () => {
    const customized = document.getElementById('customizedAvatar');
    const useBuiltIn = document.getElementById('useBuiltInVoice');
    const endpointEl = document.getElementById('customVoiceEndpointId');
    if (!useBuiltIn) return;

    // Enable Built-In only when Custom Avatar is checked AND no custom endpoint is provided
    const endpointVal = (endpointEl && typeof endpointEl.value === 'string') ? endpointEl.value.trim() : '';
    const shouldEnable = !!(customized && customized.checked && !endpointVal);

    useBuiltIn.disabled = !shouldEnable;
    if (!shouldEnable) {
        useBuiltIn.checked = false;
    }

    // Reflect and persist current states
    try {
        if (window.configManager) {
            window.configManager.config.customizedAvatar = !!(customized && customized.checked);
            window.configManager.config.useBuiltInVoice = !useBuiltIn.disabled && !!useBuiltIn.checked;
            window.configManager.persistPartial();
        }
    } catch {}

    // Ensure change on Use Built-In Voice persists
    useBuiltIn.onchange = () => {
        try {
            if (window.configManager && !useBuiltIn.disabled) {
                window.configManager.config.useBuiltInVoice = !!useBuiltIn.checked;
                window.configManager.persistPartial();
            }
        } catch {}
    };

    // Bind one-time listener: when endpoint text changes, recompute enable/disable
    if (endpointEl && !endpointEl.dataset._bindBuiltInToggle) {
        endpointEl.addEventListener('input', () => {
            window.updateCustomAvatarBox();
        });
        endpointEl.dataset._bindBuiltInToggle = '1';
    }
};

window.updateLocalVideoForIdle = () => {
    // Placeholder for local video functionality
    console.log('Local video for idle setting changed');
};

// ----------------- Avatar dropdown helpers -----------------
ConfigurationManager.prototype.setupAvatarDropdowns = function() {
    const charSel = document.getElementById('talkingAvatarCharacter');
    const styleSel = document.getElementById('talkingAvatarStyle');
    if (!charSel || !styleSel) return;

    // Poblar personajes
    charSel.innerHTML = '';
    const characters = Object.keys(this.avatarCatalog);
    for (const c of characters) {
        const opt = document.createElement('option');
        opt.value = c;
    opt.textContent = (c === 'pablitopiova') ? 'PablitoPiova'
                    : (c === 'pablitopiovaagentcon') ? 'PablitoPiovaAgentCon'
                    : capitalize(c);
        charSel.appendChild(opt);
    }

    // Listeners de dependencia
    charSel.addEventListener('change', () => {
        this.populateStylesForCharacter(charSel.value);
        // Actualizar config y seleccionar primer estilo válido
        this.config.talkingAvatarCharacter = charSel.value;
        const first = document.getElementById('talkingAvatarStyle').value;
        this.config.talkingAvatarStyle = first;
        // Auto-ajustar el modo "Custom Avatar" para personajes personalizados
        const isCustom = ['pablitopiova','pablitopiovaagentcon'].includes((charSel.value || '').toLowerCase());
        const customChk = document.getElementById('customizedAvatar');
        if (customChk) {
            customChk.checked = isCustom;
            // Persist in config and recompute Built-In Voice availability
            this.config.customizedAvatar = isCustom;
            try { this.persistPartial(); } catch {}
            if (typeof window.updateCustomAvatarBox === 'function') {
                window.updateCustomAvatarBox();
            }
        }
        this.persistPartial();
    });

    styleSel.addEventListener('change', () => {
        this.config.talkingAvatarStyle = styleSel.value;
        this.persistPartial();
    });
};

ConfigurationManager.prototype.populateStylesForCharacter = function(character) {
    const styleSel = document.getElementById('talkingAvatarStyle');
    if (!styleSel) return;
    const styles = this.avatarCatalog[character?.toLowerCase?.() || ''] || [];
    styleSel.innerHTML = '';
    for (const s of styles) {
        const opt = document.createElement('option');
        opt.value = s;
        if (!s && ['pablitopiova','pablitopiovaagentcon'].includes((character || '').toLowerCase())) {
            // Mostrar vacío para PablitoPiova y PablitoPiovaAgentCon
            opt.textContent = '';
        } else {
            opt.textContent = s ? s : '(default)';
        }
        styleSel.appendChild(opt);
    }
};

ConfigurationManager.prototype.syncAvatarDropdownsFromConfig = function() {
    const charSel = document.getElementById('talkingAvatarCharacter');
    const styleSel = document.getElementById('talkingAvatarStyle');
    if (!charSel || !styleSel) return;
    const char = (this.config.talkingAvatarCharacter || 'lisa').toLowerCase();
    // Si el personaje no existe, usa lisa
    const exists = this.avatarCatalog[char];
    const effectiveChar = exists ? char : 'lisa';
    charSel.value = effectiveChar;
    this.populateStylesForCharacter(effectiveChar);
    // Seleccionar estilo si está disponible, de lo contrario primer estilo
    const style = String(this.config.talkingAvatarStyle || '');
    const hasOption = Array.from(styleSel.options).some(o => o.value === style);
    styleSel.value = hasOption ? style : (styleSel.options[0]?.value || '');
    // Reflejar en config
    this.config.talkingAvatarCharacter = effectiveChar;
    this.config.talkingAvatarStyle = styleSel.value;
};

ConfigurationManager.prototype.persistPartial = function() {
    try {
        localStorage.setItem('azureAIFoundryConfig', JSON.stringify(this.getConfiguration()));
    } catch {}
};

function capitalize(str) {
    if (!str) return str;
    return str.charAt(0).toUpperCase() + str.slice(1);
}

// ----------------- TTS Voice dropdown helpers -----------------
ConfigurationManager.prototype.setupTtsVoiceDropdown = function() {
    const sel = document.getElementById('ttsVoice');
    if (!sel) return;
    sel.innerHTML = '';
    for (const v of this.ttsVoices) {
        const opt = document.createElement('option');
        opt.value = v;
        opt.textContent = v;
        sel.appendChild(opt);
    }
    sel.addEventListener('change', () => {
        this.config.ttsVoice = sel.value;
        this.persistPartial();
    });
};

ConfigurationManager.prototype.syncTtsVoiceFromConfig = function() {
    const sel = document.getElementById('ttsVoice');
    if (!sel) return;
    const value = this.config.ttsVoice || this.ttsVoices[0];
    const has = Array.from(sel.options).some(o => o.value === value);
    if (!has) {
        // Añadir opción personalizada si viene de .env/URL
        const opt = document.createElement('option');
        opt.value = value;
        opt.textContent = value + ' (custom)';
        sel.appendChild(opt);
    }
    sel.value = value;
};

// ----------------- Prompt profiles helpers -----------------
ConfigurationManager.prototype.setupPromptProfiles = async function() {
    const sel = document.getElementById('promptProfile');
    const status = document.getElementById('promptProfileStatus');
    if (!sel) return;
    try {
        const res = await fetch('/prompts/index.json', { cache: 'no-store' });
        if (!res.ok) throw new Error('index not found');
        const data = await res.json();
        const profiles = (data && Array.isArray(data.profiles)) ? data.profiles : [];
        this.promptProfiles = profiles;
        // Populate select
        sel.innerHTML = '<option value="">(none)</option>';
        for (const p of profiles) {
            const opt = document.createElement('option');
            opt.value = p.id;
            opt.textContent = p.name || p.id;
            sel.appendChild(opt);
        }
        // Set current value
        if (this.config.promptProfile) {
            const has = profiles.some(p => p.id === this.config.promptProfile);
            if (has) sel.value = this.config.promptProfile;
        }
        if (status) status.textContent = '';
    } catch (e) {
        if (status) status.textContent = 'No profiles found (optional).';
    }
};

ConfigurationManager.prototype.applyPromptProfileIfConfigured = async function() {
    if (!this.config.promptProfile) return;
    try {
        await this.applyPromptProfile();
    } catch {}
};

ConfigurationManager.prototype.loadPromptTemplate = async function(profileId) {
    const p = (this.promptProfiles || []).find(x => x.id === profileId);
    if (!p) throw new Error('Profile not found');
    const file = '/prompts/' + (p.file || (profileId + '.md'));
    const res = await fetch(file, { cache: 'no-store' });
    if (!res.ok) throw new Error('Template file not found: ' + file);
    const text = await res.text();
    return { template: text, defaults: p.defaults || {} };
};

ConfigurationManager.prototype.getPromptVars = function(defaults) {
    const out = { ...(defaults || {}) };
    // From config.promptVars JSON
    try {
        if (this.config.promptVars) {
            const obj = JSON.parse(this.config.promptVars);
            Object.assign(out, obj);
        }
    } catch {}
    return out;
};

ConfigurationManager.prototype.interpolateTemplate = function(template, vars) {
    let result = String(template || '');
    for (const [k, v] of Object.entries(vars || {})) {
        const re = new RegExp('{{\\s*' + k.replace(/[-/\\^$*+?.()|[\]{}]/g, '\\$&') + '\\s*}}', 'g');
        result = result.replace(re, String(v));
    }
    return result;
};

ConfigurationManager.prototype.applyPromptProfile = async function() {
    const sel = document.getElementById('promptProfile');
    const status = document.getElementById('promptProfileStatus');
    const promptEl = document.getElementById('prompt');
    const id = (sel && sel.value) || this.config.promptProfile;
    if (!id) {
        if (status) status.textContent = 'No profile selected.';
        return;
    }
    try {
        const { template, defaults } = await this.loadPromptTemplate(id);
        const vars = this.getPromptVars(defaults);
        const text = this.interpolateTemplate(template, vars);
        if (promptEl) promptEl.value = text;
        this.config.prompt = text;
        this.config.promptProfile = id;
        this.persistPartial();
        if (status) status.textContent = 'Profile applied.';
    } catch (e) {
        if (status) status.textContent = 'Failed to apply profile.';
    }
};
