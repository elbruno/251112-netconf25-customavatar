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
window.currentSpeakingText = '';
window.peerConnectionDataChannel = null;

const AVATAR_START_TIMEOUT_MS = 30000;
window.avatarStartControl = null;

function initializeAvatarStartControl(resolve, reject) {
    window.avatarStartControl = {
        resolve,
        reject,
        timeoutId: null,
        settled: false,
        startTime: Date.now()
    };
}

function settleAvatarStartControl(success, source, error) {
    const control = window.avatarStartControl;
    if (!control || control.settled) {
        return;
    }

    control.settled = true;
    if (control.timeoutId) {
        clearTimeout(control.timeoutId);
    }

    const duration = Date.now() - control.startTime;
    window.avatarStartControl = null;

    if (success) {
        console.log(`[Connection] ✅ Avatar start confirmed (${source}) in ${duration}ms`);
        if (window.peerConnection) {
            console.log('[Connection] Peer connection state:', window.peerConnection.connectionState);
            console.log('[Connection] ICE connection state:', window.peerConnection.iceConnectionState);
        }
        control.resolve();
    } else {
        const err = error instanceof Error ? error : new Error(error || 'Avatar start failed');
        const isCancellation = err && (err.name === 'AvatarStartCancelled' || err.isCancellation === true);
        const status = isCancellation ? 'cancelled' : 'failed';
        const emoji = isCancellation ? '⚠️' : '❌';
        const logFn = isCancellation ? console.warn : console.error;

        logFn(`[Connection] ${emoji} Avatar start ${status} (${source}) after ${duration}ms`, err);
        if (window.peerConnection) {
            logFn('[Connection] Peer connection state at failure:', window.peerConnection.connectionState);
            logFn('[Connection] ICE connection state at failure:', window.peerConnection.iceConnectionState);
        }
        control.reject(err);
    }
}

function clearAvatarStartControl(reason = 'manual') {
    const control = window.avatarStartControl;
    if (!control) {
        return;
    }

    if (!control.settled) {
        console.warn(`[Connection] Avatar start control cleared before settlement (${reason})`);
        const cancellationError = new Error(`Avatar start cancelled (${reason})`);
        cancellationError.name = 'AvatarStartCancelled';
        cancellationError.isCancellation = true;
        settleAvatarStartControl(false, `cleared (${reason})`, cancellationError);
        return;
    }

    if (control.timeoutId) {
        clearTimeout(control.timeoutId);
    }
    window.avatarStartControl = null;
}

// Initialize the Azure Speech SDK
window.initializeAvatarSDK = function(dotNetRef) {
    console.log('[Init] ========================================');
    console.log('[Init] INITIALIZING AVATAR SDK');
    console.log('[Init] ========================================');
    console.log('[Init] dotNetRef:', dotNetRef ? 'provided' : 'NULL');
    
    if (dotNetRef) {
        window.dotNetAvatarRef = dotNetRef;
        console.log('[Init] dotNetAvatarRef stored successfully');
    } else {
        console.error('[Init] ERROR: dotNetRef is null or undefined!');
    }
    
    console.log('[Init] Checking Speech SDK availability...');
    if (typeof SpeechSDK === 'undefined') {
        console.error('[Init] ERROR: SpeechSDK is NOT loaded! Check if the script tag is correct.');
        console.error('[Init] Script should be: https://aka.ms/csspeech/jsbrowserpackageraw');
    } else {
        console.log('[Init] ✅ SpeechSDK is available');
        console.log('[Init] SpeechSDK version:', SpeechSDK.SDK_VERSION || 'unknown');
    }
    
    console.log('[Init] Azure Speech SDK initialization complete');
};

// Start avatar session from JSON string
window.startAvatarSessionFromJson = async function(configJson) {
    console.log('[StartSession] ========================================');
    console.log('[StartSession] START AVATAR SESSION FROM JSON CALLED');
    console.log('[StartSession] ========================================');
    console.log('[StartSession] Received configJson (type):', typeof configJson);
    console.log('[StartSession] Config JSON length:', configJson ? configJson.length : 'NULL');
    
    try {
        console.log('[StartSession] Parsing JSON...');
        const config = JSON.parse(configJson);
        console.log('[StartSession] ✅ JSON parsed successfully');
        console.log('[StartSession] Config object:', config);
        console.log('[StartSession] Starting avatar session with parsed config...');
        return await startAvatarSession(config);
    } catch (error) {
        console.error('[StartSession] ❌ ERROR parsing or starting session:', error);
        console.error('[StartSession] Error message:', error.message);
        console.error('[StartSession] Error stack:', error.stack);
        throw error;
    }
};

// Start avatar session
async function startAvatarSession(config) {
    console.log('[AvatarSession] ========================================');
    console.log('[AvatarSession] STARTING AVATAR SESSION');
    console.log('[AvatarSession] ========================================');
    
    try {
        console.log('[AvatarSession] Config received:', config);
        console.log('[AvatarSession] Storing config in window.avatarAppConfig...');
        window.avatarAppConfig = config;
        
        // Get Speech SDK token
        console.log('[AvatarSession] Extracting Azure Speech credentials...');
        const region = config.azureSpeech?.region;
        const subscriptionKey = config.azureSpeech?.apiKey;
        
        console.log('[AvatarSession] Region:', region || 'MISSING');
        console.log('[AvatarSession] Subscription Key:', subscriptionKey ? '***' + subscriptionKey.slice(-4) : 'MISSING');
        
        if (!subscriptionKey || !region) {
            const error = 'Azure Speech credentials are missing! Please configure them in the Configuration page.';
            console.error('[AvatarSession] ❌ ERROR:', error);
            alert(error);
            throw new Error(error);
        }

        console.log('[AvatarSession] Building token URL for region:', region);
        const tokenUrl = `https://${region}.tts.speech.microsoft.com/cognitiveservices/avatar/relay/token/v1`;
        console.log('[AvatarSession] Token URL:', tokenUrl);

        console.log('[AvatarSession] Requesting avatar relay token...');
        const response = await fetch(tokenUrl, {
            method: 'GET',
            headers: {
                'Ocp-Apim-Subscription-Key': subscriptionKey
            }
        });

        console.log('[AvatarSession] Token response status:', response.status);
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error('[AvatarSession] ❌ Token request failed:', response.status);
            console.error('[AvatarSession] Error details:', errorText);
            throw new Error(`Failed to get avatar token: ${response.status} - ${errorText}`);
        }

        console.log('[AvatarSession] Parsing token response...');
        const tokenData = await response.json();
        console.log('[AvatarSession] ✅ Avatar token retrieved successfully');
        console.log('[AvatarSession] Token data has URLs:', tokenData.Urls ? tokenData.Urls.length : 'NONE');

        console.log('[AvatarSession] Setting up WebRTC connection...');
        await setupWebRTC(tokenData.Urls[0], tokenData.Username, tokenData.Password, config);
        
        window.sessionActive = true;
        console.log('[AvatarSession] ✅ ✅ ✅ Avatar session started successfully!');
    } catch (error) {
        console.error('[AvatarSession] ========================================');
        console.error('[AvatarSession] ❌ ❌ ❌ ERROR STARTING AVATAR SESSION');
        console.error('[AvatarSession] ========================================');
        console.error('[AvatarSession] Error type:', error.constructor.name);
        console.error('[AvatarSession] Error message:', error.message);
        console.error('[AvatarSession] Error stack:', error.stack);
        
        const errorDetails = `Failed to start avatar session: ${error.message}\n\nPlease check:\n1. Your Azure Speech credentials are correct\n2. Your subscription supports avatar features\n3. Browser console for more details`;
        alert(errorDetails);
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
        if (!window.peerConnection) {
            return;
        }

        const state = window.peerConnection.iceConnectionState;
        console.log('[ICE] Connection state changed:', state);
        if (state === 'failed') {
            console.error('[ICE] Connection failed. This may indicate network or firewall issues.');
            settleAvatarStartControl(false, 'iceConnectionState failed', new Error('ICE connection failed before avatar start completed.'));
        } else if (state === 'connected') {
            console.log('[ICE] Connection established (connected state)');
            if (window.avatarStartControl && !window.avatarStartControl.settled) {
                const peerConnected = window.peerConnection.connectionState === 'connected';
                console.log('[ICE] Peer connection state at ICE connected:', window.peerConnection.connectionState);
                if (peerConnected) {
                    settleAvatarStartControl(true, 'iceConnectionState connected');
                } else {
                    console.log('[ICE] Waiting for peerConnection connected before confirming avatar start');
                }
            }
        }
    };

    // Handle connection state changes
    window.peerConnection.onconnectionstatechange = function() {
        if (!window.peerConnection) {
            return;
        }

        const state = window.peerConnection.connectionState;
        console.log('[WebRTC] Connection state changed:', state);
        if (state === 'failed') {
            console.error('[WebRTC] Connection failed. Avatar may not load properly.');
            settleAvatarStartControl(false, 'peerConnection failed', new Error('WebRTC connection failed before avatar start completed.'));
        } else if (state === 'connected') {
            console.log('[WebRTC] Successfully connected!');
            settleAvatarStartControl(true, 'peerConnection connected');
        } else if (state === 'disconnected' && window.avatarStartControl && !window.avatarStartControl.settled) {
            console.warn('[WebRTC] Connection disconnected before avatar start completed.');
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

    // Handle data channel for subtitle events (matching Python implementation)
    window.peerConnection.addEventListener('datachannel', event => {
        console.log('[DataChannel] 📡 Data channel received:', event.channel.label);
        window.peerConnectionDataChannel = event.channel;
        
        window.peerConnectionDataChannel.onmessage = e => {
            try {
                const webRTCEvent = JSON.parse(e.data);
                const eventType = webRTCEvent?.event?.eventType;
                console.log('[DataChannel] Event received:', eventType, 'at', new Date().toISOString());
                
                const subtitlesElement = document.getElementById('subtitles');
                if (!subtitlesElement) {
                    console.warn('[Subtitle] ⚠️ Subtitle element not found in DOM');
                    return;
                }
                
                const showSubtitles = config?.avatar?.enableSubtitles !== false; // Default to true
                console.log('[Subtitle] Show subtitles enabled:', showSubtitles);
                
                if (eventType === 'EVENT_TYPE_TURN_START' && showSubtitles) {
                    subtitlesElement.hidden = false;
                    subtitlesElement.innerHTML = window.currentSpeakingText;
                    console.log('[Subtitle] ✅ Subtitle shown:', window.currentSpeakingText.substring(0, 50));
                } else if (eventType === 'EVENT_TYPE_SESSION_END' || eventType === 'EVENT_TYPE_SWITCH_TO_IDLE') {
                    subtitlesElement.hidden = true;
                    console.log('[Subtitle] ⏹️ Subtitle hidden (event:', eventType + ')');
                }
            } catch (error) {
                console.error('[DataChannel] ❌ Error processing data channel message:', error);
            }
        };
        
        window.peerConnectionDataChannel.onopen = () => {
            console.log('[DataChannel] ✅ Data channel opened');
        };
        
        window.peerConnectionDataChannel.onclose = () => {
            console.log('[DataChannel] ⏹️ Data channel closed');
        };
        
        window.peerConnectionDataChannel.onerror = (error) => {
            console.error('[DataChannel] ❌ Data channel error:', error);
        };
    });

    // Create a data channel from client side to ensure the data channel listening works
    // This is a workaround matching Python implementation
    try {
        const clientDataChannel = window.peerConnection.createDataChannel('eventChannel');
        console.log('[DataChannel] 📤 Client-side event channel created:', clientDataChannel.label);
    } catch (error) {
        console.error('[DataChannel] ❌ Failed to create client data channel:', error);
    }

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
    
    // Always use fromSubscription - it handles both standard and private endpoints correctly
    // The Speech SDK will automatically use the regional endpoint or private endpoint as configured
    const speechConfig = SpeechSDK.SpeechConfig.fromSubscription(
        config.azureSpeech.apiKey,
        config.azureSpeech.region
    );

    console.log('[Config] Speech config created successfully');

    // Configure TTS voice and custom endpoint (matching Python implementation logic)
    const customVoiceEndpointId = config.sttTts.customVoiceEndpointId || '';
    const customVoiceEndpointIdTrim = customVoiceEndpointId.trim().toLowerCase();
    const isPlaceholder = !customVoiceEndpointIdTrim || 
                         customVoiceEndpointIdTrim === 'your_custom_voice_endpoint_id' || 
                         customVoiceEndpointIdTrim.startsWith('xxxxx');
    
    console.log('[Voice] 🔧 Voice configuration analysis:');
    console.log('[Voice]   - Custom Voice Endpoint ID:', customVoiceEndpointId || '(empty)');
    console.log('[Voice]   - Is Placeholder:', isPlaceholder);
    console.log('[Voice]   - Use Built-In Voice:', config.avatar.useBuiltInVoice);
    console.log('[Voice]   - TTS Voice:', config.sttTts.ttsVoice);
    console.log('[Voice]   - Is Custom Avatar:', config.avatar.isCustomAvatar);
    
    // Only set custom endpoint if not using built-in voice and endpoint is valid
    if (!config.avatar.useBuiltInVoice && !isPlaceholder) {
        speechConfig.endpointId = customVoiceEndpointId;
        console.log('[Voice] ✅ Using custom voice endpoint:', customVoiceEndpointId);
    } else {
        speechConfig.endpointId = '';
        if (config.avatar.useBuiltInVoice) {
            console.log('[Voice] ✅ Using standard voice endpoint (built-in voice enabled)');
        } else if (isPlaceholder) {
            console.log('[Voice] ⚠️ Custom voice endpoint is placeholder - using standard endpoint');
        } else {
            console.log('[Voice] ✅ Using standard voice endpoint');
        }
    }
    
    // Note: We do NOT set speechSynthesisVoiceName here
    // Voice selection is handled in the speakText function via SSML
    console.log('[Voice] 📋 Final configuration:', {
        useBuiltInVoice: config.avatar.useBuiltInVoice,
        ttsVoice: config.sttTts.ttsVoice,
        endpointIdSet: speechConfig.endpointId ? 'custom-endpoint' : 'standard',
        isCustomAvatar: config.avatar.isCustomAvatar
    });

    // Configure avatar - handle empty style
    const avatarStyle = config.avatar.style;
    const isCustom = config.avatar.isCustomAvatar === true;
    
    console.log('[Avatar] Configuration details:');
    console.log('  - Character:', config.avatar.character);
    console.log('  - Style:', avatarStyle || '(none)');
    console.log('  - Is Custom:', isCustom);
    console.log('  - Use Built-in Voice:', config.avatar.useBuiltInVoice);
    console.log('  - Custom Voice Endpoint ID:', customVoiceEndpointId || '(none)');
    console.log('  - Speech Config Endpoint ID:', speechConfig.endpointId || '(none)');
    
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

        const description = (e?.description || '').toLowerCase();
        if (description.includes('connected') || description.includes('started')) {
            settleAvatarStartControl(true, `avatarEvent ${e.description || 'connected'}`);
        } else if ((description.includes('failed') || description.includes('error')) && window.avatarStartControl && !window.avatarStartControl.settled) {
            settleAvatarStartControl(false, `avatarEvent ${e.description || 'error'}`, new Error(e.description || 'Avatar event error'));
        }
    };

    // Start avatar connection with proper callback handling
    console.log('[Connection] Initiating avatar start sequence...');
    console.log('[Connection] Peer connection state:', window.peerConnection.connectionState);
    console.log('[Connection] ICE connection state:', window.peerConnection.iceConnectionState);
    
    return new Promise((resolve, reject) => {
        console.log('[Connection] Calling startAvatarAsync...');

        initializeAvatarStartControl(
            () => resolve(),
            (err) => reject(err)
        );

        const timeoutId = setTimeout(() => {
            console.error(`[Timeout] ❌ Avatar start timed out after ${AVATAR_START_TIMEOUT_MS / 1000} seconds`);
            if (window.peerConnection) {
                console.error('[Timeout] Peer connection state:', window.peerConnection.connectionState);
                console.error('[Timeout] ICE connection state:', window.peerConnection.iceConnectionState);
            }
            settleAvatarStartControl(false, 'timeout', new Error('Avatar start timeout - connection did not complete'));
        }, AVATAR_START_TIMEOUT_MS);

        if (window.avatarStartControl) {
            window.avatarStartControl.timeoutId = timeoutId;
        }

        window.avatarSynthesizer.startAvatarAsync(
            window.peerConnection,
            (result) => {
                console.log('[Connection] startAvatarAsync success callback invoked:', result);
                if (!result) {
                    settleAvatarStartControl(false, 'startAvatarAsync callback', new Error('No result returned from startAvatarAsync.'));
                    return;
                }

                const reason = result.reason;
                const reasonLabel = typeof reason === 'number' ? `code ${reason}` : String(reason);

                if (reason === SpeechSDK.ResultReason.SynthesizingAudioCompleted ||
                    reason === SpeechSDK.ResultReason.SynthesizingAudioStarted) {
                    console.log('[Success] startAvatarAsync callback reason:', reasonLabel);
                    settleAvatarStartControl(true, `startAvatarAsync (${reasonLabel})`);
                } else if (result.errorDetails) {
                    settleAvatarStartControl(false, 'startAvatarAsync unexpected', new Error(result.errorDetails));
                } else {
                    settleAvatarStartControl(false, 'startAvatarAsync unexpected', new Error(`Unexpected result reason: ${reasonLabel}`));
                }
            },
            (error) => {
                console.error('[Connection] startAvatarAsync error callback invoked:', error);
                const err = error instanceof Error ? error : new Error(String(error || 'Unknown error'));
                settleAvatarStartControl(false, 'startAvatarAsync error', err);
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

        console.log('[Session] 🛑 Stopping avatar session...');
        clearAvatarStartControl('session stopped');

        // Clear subtitle text
        window.currentSpeakingText = '';
        console.log('[Subtitle] Cleared speaking text on session stop');

        // Clear data channel
        if (window.peerConnectionDataChannel) {
            try {
                window.peerConnectionDataChannel.close();
                console.log('[DataChannel] Closed data channel');
            } catch (err) {
                console.warn('[DataChannel] Warning closing data channel:', err);
            }
            window.peerConnectionDataChannel = null;
        }

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
        
        // Store text for subtitle display (matching Python implementation)
        window.currentSpeakingText = text;
        console.log('[Speak] 📝 Current speaking text stored for subtitles');
        
        console.log('[Speak] Starting speech synthesis...');
        console.log('[Speak] Use built-in voice:', useBuiltInVoice);
        console.log('[Speak] TTS voice:', ttsVoice);
        console.log('[Speak] Text length:', text.length);
        console.log('[Speak] Text content:', text.substring(0, Math.min(100, text.length)));
        console.log('[Speak] Peer connection state before speak:', window.peerConnection?.connectionState);
        console.log('[Speak] ICE connection state before speak:', window.peerConnection?.iceConnectionState);
        console.log('[Speak] Avatar synthesizer state:', window.avatarSynthesizer ? 'initialized' : 'null');
        
        await new Promise((resolve, reject) => {
            const speakStartTime = Date.now();
            
            // Match Python implementation: use speakTextAsync for built-in voice, speakSsmlAsync otherwise
            if (useBuiltInVoice) {
                console.log('[Speak] Using speakTextAsync (built-in avatar voice)');
                window.avatarSynthesizer.speakTextAsync(
                    text,
                    (result) => {
                        const duration = Date.now() - speakStartTime;
                        console.log('[Speak] speakTextAsync callback invoked after', duration, 'ms');
                        console.log('[Speak] Result reason:', result?.reason, '(SynthesizingAudioCompleted=' + SpeechSDK.ResultReason.SynthesizingAudioCompleted + ')');
                        console.log('[Speak] Result ID:', result?.resultId);
                        console.log('[Speak] Peer connection state after callback:', window.peerConnection?.connectionState);
                        console.log('[Speak] ICE connection state after callback:', window.peerConnection?.iceConnectionState);
                        
                        if (result && result.reason === SpeechSDK.ResultReason.SynthesizingAudioCompleted) {
                            console.log('[Speak] ✅ Speech synthesis completed successfully (speakTextAsync)');
                            window.currentSpeakingText = ''; // Clear after completion
                            resolve();
                        } else {
                            const errorMsg = result?.errorDetails || 'Unknown error';
                            console.error('[Speak] ❌ Speech synthesis failed (speakTextAsync). Reason:', result?.reason, 'Error:', errorMsg);
                            reject(new Error(errorMsg));
                        }
                    },
                    (error) => {
                        const duration = Date.now() - speakStartTime;
                        console.error('[Speak] ❌ Error callback invoked after', duration, 'ms');
                        console.error('[Speak] Error speaking text (speakTextAsync):', error);
                        console.error('[Speak] Error type:', typeof error);
                        console.error('[Speak] Error message:', error?.message || error);
                        console.error('[Speak] Peer connection state on error:', window.peerConnection?.connectionState);
                        console.error('[Speak] ICE connection state on error:', window.peerConnection?.iceConnectionState);
                        window.currentSpeakingText = ''; // Clear on error
                        reject(error);
                    }
                );
            } else {
                // Create SSML for the text with voice tag
                const ssml = createSSML(text, ttsVoice, useBuiltInVoice);
                console.log('[Speak] Using speakSsmlAsync with custom voice');
                console.log('[Speak] SSML:', ssml.substring(0, Math.min(200, ssml.length)));
                
                window.avatarSynthesizer.speakSsmlAsync(
                    ssml,
                    (result) => {
                        const duration = Date.now() - speakStartTime;
                        console.log('[Speak] speakSsmlAsync callback invoked after', duration, 'ms');
                        console.log('[Speak] Result reason:', result?.reason, '(SynthesizingAudioCompleted=' + SpeechSDK.ResultReason.SynthesizingAudioCompleted + ')');
                        console.log('[Speak] Result ID:', result?.resultId);
                        console.log('[Speak] Peer connection state after callback:', window.peerConnection?.connectionState);
                        console.log('[Speak] ICE connection state after callback:', window.peerConnection?.iceConnectionState);
                        
                        if (result && result.reason === SpeechSDK.ResultReason.SynthesizingAudioCompleted) {
                            console.log('[Speak] ✅ Speech synthesis completed successfully (speakSsmlAsync)');
                            window.currentSpeakingText = ''; // Clear after completion
                            resolve();
                        } else {
                            const errorMsg = result?.errorDetails || 'Unknown error';
                            console.error('[Speak] ❌ Speech synthesis failed (speakSsmlAsync). Reason:', result?.reason, 'Error:', errorMsg);
                            reject(new Error(errorMsg));
                        }
                    },
                    (error) => {
                        const duration = Date.now() - speakStartTime;
                        console.error('[Speak] ❌ Error callback invoked after', duration, 'ms');
                        console.error('[Speak] Error speaking SSML (speakSsmlAsync):', error);
                        console.error('[Speak] Error type:', typeof error);
                        console.error('[Speak] Error message:', error?.message || error);
                        console.error('[Speak] Peer connection state on error:', window.peerConnection?.connectionState);
                        console.error('[Speak] ICE connection state on error:', window.peerConnection?.iceConnectionState);
                        window.currentSpeakingText = ''; // Clear on error
                        reject(error);
                    }
                );
            }
        });
        
        console.log('[Speak] ✅ Speech synthesis promise resolved');
    } catch (error) {
        console.error('[Speak] ❌ Error in speakText:', error);
        console.error('[Speak] Error stack:', error?.stack);
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

// Download file helper for configuration export
window.downloadFile = function(filename, content) {
    try {
        const blob = new Blob([content], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = filename;
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
        URL.revokeObjectURL(url);
        console.log('[File] Downloaded:', filename);
    } catch (error) {
        console.error('[File] Error downloading file:', error);
        throw error;
    }
};

// Scroll chat container to bottom
window.scrollChatToBottom = function() {
    try {
        const chatContainer = document.getElementById('chatContainer');
        if (chatContainer) {
            chatContainer.scrollTop = chatContainer.scrollHeight;
        }
    } catch (error) {
        console.error('[Chat] Error scrolling to bottom:', error);
    }
};
