// Azure Speech SDK Avatar Integration for Blazor
// This JavaScript file provides interop between Blazor and the Azure Speech SDK

window.avatarSynthesizer = null;
window.speechRecognizer = null;
window.peerConnection = null;
window.webAudioCtx = null;
window.webAudioGainNode = null;
window.sessionActive = false;
window.avatarAppConfig = null;
window.dotNetAvatarRef = null;
window.microphoneActive = false;

// Initialize the Azure Speech SDK
window.initializeAvatarSDK = function(dotNetRef) {
    if (dotNetRef) {
        window.dotNetAvatarRef = dotNetRef;
    }
    console.log('Azure Speech SDK initialized');
};

// Start avatar session from JSON string
window.startAvatarSessionFromJson = async function(configJson) {
    const config = JSON.parse(configJson);
    console.log('Starting avatar session with config:', config);
    return await startAvatarSession(config);
};

// Start avatar session
async function startAvatarSession(config) {
    try {
        console.log('Starting avatar session...', config);
        window.avatarAppConfig = config;
        
        // Get Speech SDK token
        const region = config.azureSpeech.region;
        const subscriptionKey = config.azureSpeech.apiKey;
        
        if (!subscriptionKey || !region) {
            const error = 'Please configure Azure Speech credentials in the Configuration page.';
            console.error(error);
            alert(error);
            return;
        }

        console.log(`Requesting avatar token from region: ${region}`);

        // Request avatar relay token
        const tokenUrl = `https://${region}.tts.speech.microsoft.com/cognitiveservices/avatar/relay/token/v1`;
        
        const response = await fetch(tokenUrl, {
            method: 'GET',
            headers: {
                'Ocp-Apim-Subscription-Key': subscriptionKey
            }
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to get avatar token: ${response.status} - ${errorText}`);
        }

        const tokenData = await response.json();
        console.log('Avatar token retrieved successfully');

        // Setup WebRTC connection
        await setupWebRTC(tokenData.Urls[0], tokenData.Username, tokenData.Password, config);
        
        window.sessionActive = true;
        console.log('Avatar session started successfully');
    } catch (error) {
        console.error('Error starting avatar session:', error);
        alert('Failed to start avatar session: ' + error.message + '\n\nPlease check:\n1. Your Azure Speech credentials are correct\n2. Your subscription supports avatar features\n3. Browser console for more details');
        throw error;
    }
}

// Setup WebRTC connection
async function setupWebRTC(iceServerUrl, username, password, config) {
    console.log('Setting up WebRTC connection...');
    console.log('Avatar config:', { 
        character: config.avatar.character, 
        style: config.avatar.style,
        isCustom: config.avatar.isCustomAvatar 
    });

    // Clean up any existing connection before creating a new one
    if (window.peerConnection) {
        try {
            console.warn('[WebRTC] Existing peer connection found. Closing before creating a new one.');
            window.peerConnection.ontrack = null;
            window.peerConnection.onicecandidate = null;
            window.peerConnection.oniceconnectionstatechange = null;
            window.peerConnection.onconnectionstatechange = null;
            window.peerConnection.onsignalingstatechange = null;
            window.peerConnection.onicegatheringstatechange = null;
            window.peerConnection.close();
        } catch (cleanupError) {
            console.error('[WebRTC] Error closing existing peer connection:', cleanupError);
        }
    }

    // Create peer connection
    window.peerConnection = new RTCPeerConnection({
        iceServers: [{
            urls: [iceServerUrl],
            username: username,
            credential: password
        }],
        bundlePolicy: 'max-bundle',
        iceTransportPolicy: 'all',
        sdpSemantics: 'unified-plan'
    });

    console.log('Peer connection created');

    const ensureTransceiver = (kind) => {
        try {
            const transceiver = window.peerConnection.addTransceiver(kind, { direction: 'recvonly' });
            console.log(`[WebRTC] ${kind} transceiver added (recvonly) - mid:`, transceiver?.mid ?? 'n/a');
            return transceiver;
        } catch (recvOnlyError) {
            console.warn(`[WebRTC] Unable to add ${kind} transceiver with recvonly. Retrying with default direction.`, recvOnlyError);
            const fallbackTransceiver = window.peerConnection.addTransceiver(kind);
            console.log(`[WebRTC] ${kind} transceiver added (default direction) - mid:`, fallbackTransceiver?.mid ?? 'n/a');
            return fallbackTransceiver;
        }
    };

    try {
        ensureTransceiver('video');
        ensureTransceiver('audio');
    } catch (error) {
        console.error('[WebRTC] ❌ Failed to add media transceivers:', error);
        throw error;
    }

    // Handle incoming video/audio tracks
    window.peerConnection.ontrack = function(event) {
        console.log('[Track] Received track:', event.track.kind, 'Ready state:', event.track.readyState);
        console.log('[Track] Streams count:', event.streams.length);
        
        if (event.track.kind === 'video') {
            console.log('[Video] Setting up video element...');
            console.log('[Video] Stream ID:', event.streams[0].id);
            console.log('[Video] Stream active:', event.streams[0].active);
            console.log('[Video] Video tracks:', event.streams[0].getVideoTracks().length);
            
            let videoElement = document.createElement('video');
            videoElement.id = 'videoPlayer';
            videoElement.srcObject = event.streams[0];
            videoElement.autoplay = true;
            videoElement.playsInline = true;
            videoElement.style.width = '100%';
            videoElement.style.height = 'auto';

            // Add event listeners to track video loading
            videoElement.onloadedmetadata = function() {
                console.log('[Video] Metadata loaded - dimensions:', videoElement.videoWidth, 'x', videoElement.videoHeight);
            };
            
            videoElement.onloadeddata = function() {
                console.log('[Video] Data loaded, ready to play');
            };
            
            videoElement.onplay = function() {
                console.log('[Video] ✅ Video playing!');
            };
            
            videoElement.onerror = function(e) {
                console.error('[Video] ❌ Error loading video:', e);
            };

            const remoteVideo = document.getElementById('remoteVideo');
            if (remoteVideo) {
                // Clear existing video
                remoteVideo.innerHTML = '';
                remoteVideo.appendChild(videoElement);
                console.log('[Video] Video element added to DOM');
            } else {
                console.error('[Video] ❌ remoteVideo element not found in DOM!');
            }
        } else if (event.track.kind === 'audio') {
            console.log('[Audio] Setting up audio element...');
            console.log('[Audio] Stream ID:', event.streams[0].id);
            console.log('[Audio] Stream active:', event.streams[0].active);
            console.log('[Audio] Audio tracks:', event.streams[0].getAudioTracks().length);
            
            let audioElement = document.createElement('audio');
            audioElement.id = 'audioPlayer';
            audioElement.srcObject = event.streams[0];
            audioElement.autoplay = true;

            // Setup Web Audio API for gain control
            const audioGain = config.avatar.audioGain || 1.8;
            console.log('[Audio] Setting up audio gain:', audioGain);
            setupAudioGain(audioElement, event.streams[0], audioGain);

            const remoteVideo = document.getElementById('remoteVideo');
            if (remoteVideo) {
                remoteVideo.appendChild(audioElement);
                console.log('[Audio] Audio element added to DOM');
            } else {
                console.error('[Audio] ❌ remoteVideo element not found in DOM!');
            }
        }
    };

    // Handle ICE connection state changes
    window.peerConnection.oniceconnectionstatechange = function() {
        console.log('[ICE] Connection state changed:', window.peerConnection.iceConnectionState);
        if (window.peerConnection.iceConnectionState === 'failed') {
            console.error('[ICE] Connection failed. This may indicate network or firewall issues.');
        }
    };

    // Handle connection state changes
    window.peerConnection.onconnectionstatechange = function() {
        console.log('[WebRTC] Connection state changed:', window.peerConnection.connectionState);
        if (window.peerConnection.connectionState === 'failed') {
            console.error('[WebRTC] Connection failed. Avatar may not load properly.');
        } else if (window.peerConnection.connectionState === 'connected') {
            console.log('[WebRTC] Successfully connected!');
        }
    };

    // Handle ICE gathering state
    window.peerConnection.onicegatheringstatechange = function() {
        console.log('[ICE] Gathering state:', window.peerConnection.iceGatheringState);
    };

    // Handle ICE candidates
    window.peerConnection.onicecandidate = function(event) {
        if (event.candidate) {
            console.log('[ICE] New candidate:', event.candidate.type, event.candidate.protocol);
        } else {
            console.log('[ICE] All candidates gathered');
        }
    };

    // Handle signaling state
    window.peerConnection.onsignalingstatechange = function() {
        console.log('[WebRTC] Signaling state:', window.peerConnection.signalingState);
    };

    // Verify Speech SDK is loaded
    if (typeof SpeechSDK === 'undefined') {
        console.error('[SDK] Speech SDK not found!');
        throw new Error('Speech SDK not loaded. Please refresh the page.');
    }
    console.log('[SDK] Speech SDK version:', SpeechSDK.SDK_VERSION || 'unknown');

    // Create and initialize avatar synthesizer
    console.log('[Config] Creating speech config...');
    console.log('[Config] Region:', config.azureSpeech.region);
    console.log('[Config] Enable private endpoint:', config.azureSpeech.enablePrivateEndpoint);
    console.log('[Config] Private endpoint:', config.azureSpeech.privateEndpoint || '(not set)');
    
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

    console.log('[Config] Speech config created successfully');

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
    console.log('[Voice] Configuration:', {
        useBuiltInVoice: config.avatar.useBuiltInVoice,
        ttsVoice: config.sttTts.ttsVoice,
        endpointIdUsed: speechConfig.endpointId ? 'custom-endpoint' : 'standard'
    });

    // Configure avatar - handle empty style
    const avatarStyle = config.avatar.style;
    const isCustom = config.avatar.isCustomAvatar === true;
    
    console.log('[Avatar] Configuration details:');
    console.log('  - Character:', config.avatar.character);
    console.log('  - Style:', avatarStyle || '(none)');
    console.log('  - Is Custom:', isCustom);
    console.log('  - Use Built-in Voice:', config.avatar.useBuiltInVoice);
    
    // For custom avatars or when style is empty/null, don't pass style parameter
    let avatarConfig;
    if (!avatarStyle || avatarStyle === '') {
        console.log('[Avatar] Creating avatar config WITHOUT style (recommended for custom avatars)');
        avatarConfig = new SpeechSDK.AvatarConfig(config.avatar.character);
    } else {
        console.log('[Avatar] Creating avatar config WITH style:', avatarStyle);
        avatarConfig = new SpeechSDK.AvatarConfig(config.avatar.character, avatarStyle);
    }
    
    // Set customized flag for custom avatars
    avatarConfig.customized = isCustom;
    console.log('[Avatar] Customized flag set to:', avatarConfig.customized);

    const videoFormat = new SpeechSDK.AvatarVideoFormat();
    videoFormat.bitrate = 2000000;
    console.log('[Avatar] Video format configured - bitrate:', videoFormat.bitrate);

    console.log('[Synthesizer] Creating avatar synthesizer with video format...');
    window.avatarSynthesizer = new SpeechSDK.AvatarSynthesizer(
        speechConfig,
        avatarConfig,
        videoFormat
    );
    console.log('[Synthesizer] Avatar synthesizer created successfully');

    // Set up avatar event handler before starting
    window.avatarSynthesizer.avatarEventReceived = function(s, e) {
        console.log('[Avatar Event] Offset:', e.offset, 'Description:', e.description);
    };

    // Start avatar connection with proper callback handling
    console.log('[Connection] Initiating avatar start sequence...');
    console.log('[Connection] Peer connection state:', window.peerConnection.connectionState);
    console.log('[Connection] ICE connection state:', window.peerConnection.iceConnectionState);
    
    return new Promise((resolve, reject) => {
        const startTime = Date.now();
        console.log('[Connection] Calling startAvatarAsync...');
        
        // Set a timeout in case the callback never fires
        const timeout = setTimeout(() => {
            console.error('[Timeout] ❌ Avatar start timed out after 30 seconds');
            console.error('[Timeout] Peer connection state:', window.peerConnection.connectionState);
            console.error('[Timeout] ICE connection state:', window.peerConnection.iceConnectionState);
            reject(new Error('Avatar start timeout - connection did not complete'));
        }, 30000);
        
        window.avatarSynthesizer.startAvatarAsync(
            window.peerConnection,
            (result) => {
                clearTimeout(timeout);
                const duration = Date.now() - startTime;
                
                if (result.reason === SpeechSDK.ResultReason.SynthesizingAudioCompleted) {
                    console.log(`[Success] ✅ Avatar started successfully in ${duration}ms!`);
                    console.log('[Success] Result reason:', result.reason);
                    console.log('[Success] Final peer connection state:', window.peerConnection.connectionState);
                    console.log('[Success] Final ICE connection state:', window.peerConnection.iceConnectionState);
                    resolve();
                } else {
                    console.error(`[Error] ❌ Avatar start returned unexpected reason: ${result.reason}`);
                    console.error('[Error] Error details:', result.errorDetails);
                    reject(new Error('Avatar start failed: ' + result.errorDetails));
                }
            },
            (error) => {
                clearTimeout(timeout);
                const duration = Date.now() - startTime;
                console.error(`[Error] ❌ Avatar start failed after ${duration}ms`);
                console.error('[Error] Error details:', error);
                console.error('[Error] Error type:', typeof error);
                console.error('[Error] Error string:', String(error));
                console.error('[Error] Peer connection state:', window.peerConnection.connectionState);
                console.error('[Error] ICE connection state:', window.peerConnection.iceConnectionState);
                reject(new Error('Failed to start avatar: ' + error));
            }
        );
        
        console.log('[Connection] startAvatarAsync called, waiting for callback...');
    });
}

// Setup audio gain control
function setupAudioGain(audioElement, stream, gain) {
    try {
        if (!window.webAudioCtx) {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            window.webAudioCtx = new AudioContext();
        }

        // Mute the audio element to avoid double playback
        audioElement.muted = true;

        // Create audio source from stream
        const source = window.webAudioCtx.createMediaStreamSource(stream);
        window.webAudioGainNode = window.webAudioCtx.createGain();
        window.webAudioGainNode.gain.value = Math.max(0.1, Math.min(gain, 5.0));

        // Connect the nodes
        source.connect(window.webAudioGainNode);
        window.webAudioGainNode.connect(window.webAudioCtx.destination);

        console.log('Audio gain set to:', window.webAudioGainNode.gain.value);
    } catch (error) {
        console.error('Error setting up audio gain:', error);
        audioElement.muted = false;
        audioElement.volume = 1.0;
    }
}

// Stop avatar session
window.stopAvatarSession = async function() {
    try {
        await window.stopMicrophone(true);

        if (window.avatarSynthesizer) {
            window.avatarSynthesizer.close();
            window.avatarSynthesizer = null;
        }

        if (window.speechRecognizer) {
            window.speechRecognizer.stopContinuousRecognitionAsync();
            window.speechRecognizer.close();
            window.speechRecognizer = null;
        }

        if (window.peerConnection) {
            window.peerConnection.close();
            window.peerConnection = null;
        }

        if (window.webAudioGainNode) {
            window.webAudioGainNode.disconnect();
            window.webAudioGainNode = null;
        }

        if (window.webAudioCtx) {
            await window.webAudioCtx.close();
            window.webAudioCtx = null;
        }

        // Clear video container
        const remoteVideo = document.getElementById('remoteVideo');
        if (remoteVideo) {
            remoteVideo.innerHTML = '';
        }

        window.sessionActive = false;
        window.avatarAppConfig = null;
        console.log('Avatar session stopped');
    } catch (error) {
        console.error('Error stopping avatar session:', error);
    }
};

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

// Speak text using avatar
window.speakText = async function(text) {
    if (!window.avatarSynthesizer) {
        console.error('[Speak] Avatar synthesizer not initialized');
        return;
    }

    if (!window.avatarAppConfig) {
        console.error('[Speak] Avatar configuration not available');
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
        console.log('[Speak] Text length:', text.length);
        
        await new Promise((resolve, reject) => {
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

// Stop speaking
window.stopSpeaking = function() {
    if (window.avatarSynthesizer) {
        window.avatarSynthesizer.stopSpeakingAsync();
    }
};

// Start microphone for speech recognition
window.startMicrophone = async function() {
    if (!window.sessionActive) {
        alert('Please start an avatar session first');
        return;
    }

    if (window.microphoneActive) {
        console.warn('[Microphone] Microphone already running');
        return;
    }

    if (!window.avatarAppConfig) {
        alert('Avatar configuration not available. Please restart the avatar session.');
        return;
    }

    if (typeof SpeechSDK === 'undefined') {
        alert('Speech SDK not available. Please refresh the page.');
        return;
    }

    try {
        const speechKey = window.avatarAppConfig.azureSpeech.apiKey;
        const speechRegion = window.avatarAppConfig.azureSpeech.region;

        if (!speechKey || !speechRegion) {
            alert('Missing Azure Speech credentials. Please configure them in the Configuration page.');
            return;
        }

        console.log('[Microphone] Starting microphone with region:', speechRegion);

        // Dispose any existing recognizer
        if (window.speechRecognizer) {
            try {
                window.speechRecognizer.stopContinuousRecognitionAsync();
                window.speechRecognizer.close();
            } catch (disposeError) {
                console.warn('[Microphone] Warning disposing previous recognizer:', disposeError);
            }
            window.speechRecognizer = null;
        }

        const speechConfig = SpeechSDK.SpeechConfig.fromSubscription(speechKey, speechRegion);

        // Set recognition language (use first locale specified)
        const locales = (window.avatarAppConfig.sttTts?.sttLocales || 'en-US')
            .split(',')
            .map(l => l.trim())
            .filter(l => l.length > 0);
        const recognitionLanguage = locales.length > 0 ? locales[0] : 'en-US';
        speechConfig.speechRecognitionLanguage = recognitionLanguage;
        console.log('[Microphone] Recognition language:', recognitionLanguage);

        // Use custom voice endpoint if provided
        if (window.avatarAppConfig.sttTts?.customVoiceEndpointId) {
            speechConfig.endpointId = window.avatarAppConfig.sttTts.customVoiceEndpointId;
            console.log('[Microphone] Using custom voice endpoint:', speechConfig.endpointId);
        }

        const audioConfig = SpeechSDK.AudioConfig.fromDefaultMicrophoneInput();
        window.speechRecognizer = new SpeechSDK.SpeechRecognizer(speechConfig, audioConfig);

        window.speechRecognizer.recognizing = function(sender, event) {
            console.log('[Microphone] Recognizing:', event?.result?.text || '(no text yet)');
        };

        window.speechRecognizer.recognized = function(sender, event) {
            if (!event || !event.result) return;

            if (event.result.reason === SpeechSDK.ResultReason.RecognizedSpeech) {
                const text = event.result.text;
                if (text) {
                    console.log('[Microphone] ✅ Recognized text:', text);
                    if (window.dotNetAvatarRef) {
                        window.dotNetAvatarRef.invokeMethodAsync('OnSpeechRecognized', text)
                            .catch(err => console.error('[Microphone] Error invoking .NET callback:', err));
                    }
                }
            } else if (event.result.reason === SpeechSDK.ResultReason.NoMatch) {
                console.warn('[Microphone] ❔ No speech recognized');
            }
        };

        window.speechRecognizer.canceled = function(sender, event) {
            console.error('[Microphone] ❌ Recognition canceled:', event?.errorDetails || 'Unknown error');
            if (window.dotNetAvatarRef) {
                window.dotNetAvatarRef.invokeMethodAsync('OnMicrophoneStateChanged', false)
                    .catch(err => console.error('[Microphone] Error notifying .NET of cancellation:', err));
            }
            window.microphoneActive = false;
        };

        window.speechRecognizer.sessionStarted = function() {
            console.log('[Microphone] 🎤 Session started');
        };

        window.speechRecognizer.sessionStopped = function() {
            console.log('[Microphone] 🛑 Session stopped');
            window.microphoneActive = false;
            if (window.dotNetAvatarRef) {
                window.dotNetAvatarRef.invokeMethodAsync('OnMicrophoneStateChanged', false)
                    .catch(err => console.error('[Microphone] Error notifying .NET after stop:', err));
            }
        };

        await new Promise((resolve, reject) => {
            window.speechRecognizer.startContinuousRecognitionAsync(
                () => {
                    console.log('[Microphone] ✅ Continuous recognition started');
                    window.microphoneActive = true;
                    if (window.dotNetAvatarRef) {
                        window.dotNetAvatarRef.invokeMethodAsync('OnMicrophoneStateChanged', true)
                            .catch(err => console.error('[Microphone] Error notifying .NET of start:', err));
                    }
                    resolve();
                },
                (error) => {
                    console.error('[Microphone] ❌ Failed to start recognition:', error);
                    reject(error);
                }
            );
        });
    } catch (error) {
        console.error('[Microphone] Error starting microphone:', error);
        alert('Failed to start microphone: ' + error);
        throw error;
    }
};

window.stopMicrophone = async function(isInternalCall = false) {
    if (!window.speechRecognizer) {
        if (!isInternalCall) {
            console.warn('[Microphone] No active microphone session to stop');
        }
        return;
    }

    try {
        await new Promise((resolve, reject) => {
            window.speechRecognizer.stopContinuousRecognitionAsync(
                () => {
                    console.log('[Microphone] ⏹️ Continuous recognition stopped');
                    window.microphoneActive = false;
                    if (!isInternalCall && window.dotNetAvatarRef) {
                        window.dotNetAvatarRef.invokeMethodAsync('OnMicrophoneStateChanged', false)
                            .catch(err => console.error('[Microphone] Error notifying .NET after stop:', err));
                    }
                    resolve();
                },
                (error) => {
                    console.error('[Microphone] ❌ Failed to stop recognition:', error);
                    reject(error);
                }
            );
        });
    } catch (error) {
        console.error('[Microphone] Error stopping microphone:', error);
        throw error;
    } finally {
        try {
            window.speechRecognizer.close();
        } catch (disposeError) {
            console.warn('[Microphone] Warning disposing recognizer:', disposeError);
        }
        window.speechRecognizer = null;
    }
};
