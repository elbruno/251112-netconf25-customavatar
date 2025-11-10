// Environment configuration loader - EXAMPLE FILE
// Copy this file to js/env.js and fill in your actual values
// This file is ignored by git for security

// IMPORTANT: Only include non-sensitive configuration here in production
// API keys should be handled securely in production environments

window.ENV_CONFIG = {
    // Non-sensitive defaults only
    AZURE_SPEECH_REGION: 'westus2',
    TTS_VOICE: 'en-US-AvaMultilingualNeural'
};

// Function to load environment variables in browser
function loadEnvironmentVariables() {
    console.log('Loading environment configuration...');
    return window.ENV_CONFIG || {};
}
