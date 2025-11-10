# Avatar Voice and WebRTC Connection Fix - Technical Summary

## Problem Statement

The Blazor application was experiencing issues with avatar voice configuration and WebRTC connections. The avatar would not properly use the configured voice settings, resulting in either:
1. Avatar not loading
2. Wrong voice being used
3. Connection timeouts
4. Voice conflicts between built-in and custom voices

## Root Cause Analysis

After comparing the Blazor implementation with the working JavaScript demo, the following issues were identified:

### Issue 1: Incorrect Voice Configuration
**Problem**: The code was setting `speechConfig.speechSynthesisVoiceName` directly on the SpeechConfig object.

```javascript
// INCORRECT (old code)
if (!config.avatar.useBuiltInVoice && config.sttTts.ttsVoice) {
    speechConfig.speechSynthesisVoiceName = config.sttTts.ttsVoice;
}
```

**Why This Was Wrong**: The Azure Speech SDK handles avatar voices through a different mechanism than regular TTS. Setting `speechSynthesisVoiceName` conflicts with the avatar's voice system.

### Issue 2: Missing SSML Support
**Problem**: The code was using `speakTextAsync()` with plain text, which doesn't provide proper voice control for avatars.

```javascript
// INCORRECT (old code)
window.avatarSynthesizer.speakTextAsync(text, ...)
```

**Why This Was Wrong**: Avatar voice selection requires SSML with proper voice tags. Plain text doesn't allow the SDK to distinguish between built-in and custom voices.

### Issue 3: No Custom Endpoint Validation
**Problem**: The code wasn't validating custom voice endpoint IDs, leading to errors when placeholder values were used.

**Why This Was Wrong**: Empty or placeholder endpoint IDs would cause API errors when attempting to use custom voices.

### Issue 4: Missing Private Endpoint Support
**Problem**: The code always used standard subscription endpoints, ignoring private endpoint configuration.

**Why This Was Wrong**: Organizations using private endpoints couldn't connect to their Speech resources.

## Solution Implementation

### Fix 1: Proper Voice Configuration

**Removed** the `speechSynthesisVoiceName` setting and instead handle voice selection through SSML:

```javascript
// CORRECT (new code)
// Configure TTS voice and custom endpoint
const customVoiceEndpointId = config.sttTts.customVoiceEndpointId || '';
const customVoiceEndpointIdTrim = customVoiceEndpointId.trim().toLowerCase();
const isPlaceholder = !customVoiceEndpointIdTrim || 
                     customVoiceEndpointIdTrim === 'your_custom_voice_endpoint_id' || 
                     customVoiceEndpointIdTrim.startsWith('xxxxx');

// Only set custom endpoint if not using built-in voice and endpoint is valid
if (!config.avatar.useBuiltInVoice && !isPlaceholder) {
    speechConfig.endpointId = customVoiceEndpointId;
    console.log('[Voice] Using custom voice endpoint:', customVoiceEndpointId);
} else {
    speechConfig.endpointId = '';
    console.log('[Voice] Using standard voice endpoint');
}

// Note: We do NOT set speechSynthesisVoiceName here
// Voice selection is handled in the speakText function via SSML
```

### Fix 2: SSML Support

**Added** helper functions and proper SSML generation:

```javascript
// HTML encode function for SSML
function htmlEncode(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Create SSML wrapper for text
function createSSML(text, ttsVoice, useBuiltInVoice, endingSilenceMs = 0) {
    // If using built-in avatar voice, use simple SSML without voice tag
    if (useBuiltInVoice) {
        if (endingSilenceMs > 0) {
            return `<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'><mstts:leadingsilence-exact value='0'/>${htmlEncode(text)}<break time='${endingSilenceMs}ms' /></speak>`;
        } else {
            return `<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'><mstts:leadingsilence-exact value='0'/>${htmlEncode(text)}</speak>`;
        }
    }
    
    // Use specified voice with voice tag
    if (endingSilenceMs > 0) {
        return `<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'><voice name='${ttsVoice}'><mstts:leadingsilence-exact value='0'/>${htmlEncode(text)}<break time='${endingSilenceMs}ms' /></voice></speak>`;
    } else {
        return `<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'><voice name='${ttsVoice}'><mstts:leadingsilence-exact value='0'/>${htmlEncode(text)}</voice></speak>`;
    }
}
```

**Updated** the `speakText` function to use SSML:

```javascript
window.speakText = async function(text) {
    if (!window.avatarSynthesizer || !window.avatarAppConfig) {
        console.error('[Speak] Avatar not initialized');
        return;
    }

    try {
        const useBuiltInVoice = window.avatarAppConfig.avatar.useBuiltInVoice;
        const ttsVoice = window.avatarAppConfig.sttTts.ttsVoice || 'en-US-AvaMultilingualNeural';
        
        // Create SSML for the text
        const ssml = createSSML(text, ttsVoice, useBuiltInVoice);
        
        console.log('[Speak] Starting speech synthesis...');
        console.log('[Speak] Use built-in voice:', useBuiltInVoice);
        console.log('[Speak] TTS voice:', ttsVoice);
        
        await new Promise((resolve, reject) => {
            // Use speakSsmlAsync instead of speakTextAsync
            window.avatarSynthesizer.speakSsmlAsync(
                ssml,
                (result) => {
                    if (result.reason === SpeechSDK.ResultReason.SynthesizingAudioCompleted) {
                        console.log('[Speak] ✅ Speech synthesis completed');
                        resolve();
                    } else {
                        console.error('[Speak] ❌ Speech synthesis failed:', result.errorDetails);
                        reject(new Error(result.errorDetails));
                    }
                },
                (error) => {
                    console.error('[Speak] ❌ Error speaking text:', error);
                    reject(error);
                }
            );
        });
    } catch (error) {
        console.error('[Speak] ❌ Error in speakText:', error);
        throw error;
    }
};
```

### Fix 3: Private Endpoint Support

**Added** proper WebSocket URL construction for private endpoints:

```javascript
let speechConfig;
if (config.azureSpeech.enablePrivateEndpoint && config.azureSpeech.privateEndpoint) {
    // Use private endpoint
    const privateEndpoint = config.azureSpeech.privateEndpoint;
    // Extract the host part and construct WebSocket URL
    const endpointHost = privateEndpoint.startsWith('https://') 
        ? privateEndpoint.slice(8) 
        : privateEndpoint;
    const wsUrl = `wss://${endpointHost}/tts/cognitiveservices/websocket/v1?enableTalkingAvatar=true`;
    console.log('[Config] Using private endpoint WebSocket URL:', wsUrl);
    speechConfig = SpeechSDK.SpeechConfig.fromEndpoint(new URL(wsUrl), config.azureSpeech.apiKey);
} else {
    // Use standard subscription
    console.log('[Config] Using standard subscription endpoint');
    speechConfig = SpeechSDK.SpeechConfig.fromSubscription(
        config.azureSpeech.apiKey,
        config.azureSpeech.region
    );
}
```

## How Voice Selection Works Now

### Scenario 1: Using Built-in Avatar Voice
**Configuration**: `useBuiltInVoice = true`

**SSML Generated**:
```xml
<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' 
       xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'>
    <mstts:leadingsilence-exact value='0'/>
    Hello, world!
</speak>
```

**Result**: Avatar speaks with its built-in voice (e.g., Lisa's voice for Lisa avatar)

### Scenario 2: Using Custom TTS Voice
**Configuration**: `useBuiltInVoice = false`, `ttsVoice = "en-US-AvaMultilingualNeural"`

**SSML Generated**:
```xml
<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' 
       xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'>
    <voice name='en-US-AvaMultilingualNeural'>
        <mstts:leadingsilence-exact value='0'/>
        Hello, world!
    </voice>
</speak>
```

**Result**: Avatar lip-syncs to the specified TTS voice

## Configuration Examples

### Example 1: Standard Avatar (Lisa) with Built-in Voice
```json
{
  "Avatar": {
    "Character": "lisa",
    "Style": "casual-sitting",
    "UseBuiltInVoice": true
  },
  "SttTts": {
    "TtsVoice": "en-US-AvaMultilingualNeural"
  }
}
```

### Example 2: Custom Avatar with Custom Voice
```json
{
  "Avatar": {
    "Character": "Bruno-Avatar-02",
    "Style": "",
    "UseBuiltInVoice": false,
    "IsCustomAvatar": true
  },
  "SttTts": {
    "TtsVoice": "en-US-AvaMultilingualNeural",
    "CustomVoiceEndpointId": ""
  }
}
```

### Example 3: Private Endpoint Configuration
```json
{
  "AzureSpeech": {
    "Region": "westus2",
    "ApiKey": "your-api-key",
    "EnablePrivateEndpoint": true,
    "PrivateEndpoint": "https://westus2.tts.speech.microsoft.com/cognitiveservices/websocket/v1"
  }
}
```

## Benefits of This Fix

1. **Proper Voice Routing**: Avatar voices and TTS voices are now correctly distinguished
2. **Better Error Handling**: Placeholder validation prevents API errors
3. **Private Endpoint Support**: Organizations can use private endpoints
4. **Enhanced Logging**: Comprehensive logging helps debug issues
5. **Security**: HTML encoding prevents SSML injection attacks
6. **Flexibility**: Supports all avatar voice scenarios

## Testing Performed

1. ✅ Build validation - no errors or warnings
2. ✅ CodeQL security scan - no vulnerabilities found
3. ✅ SSML generation validation - all test cases pass
4. ✅ HTML encoding test - special characters properly escaped

## Migration Guide

No code changes required for users of the Blazor application. The fix is transparent and maintains backward compatibility with existing configurations.

### For Developers

If you're maintaining similar code, remember:
- **Never** set `speechSynthesisVoiceName` when working with avatars
- **Always** use SSML for avatar speech synthesis
- **Always** validate custom endpoint IDs before use
- **Always** properly construct WebSocket URLs for private endpoints

## Related Files

- `dotnet/AzureAIAvatarBlazor/wwwroot/js/avatar-interop.js` - Main fix implementation
- `dotnet/AzureAIAvatarBlazor/appsettings.json` - Configuration template
- `dotnet/AzureAIAvatarBlazor/appsettings.Development.json` - Development settings

## References

- [Azure Speech SDK Documentation](https://docs.microsoft.com/azure/cognitive-services/speech-service/)
- [SSML Reference](https://docs.microsoft.com/azure/cognitive-services/speech-service/speech-synthesis-markup)
- [Avatar API Documentation](https://docs.microsoft.com/azure/cognitive-services/speech-service/avatar-overview)
