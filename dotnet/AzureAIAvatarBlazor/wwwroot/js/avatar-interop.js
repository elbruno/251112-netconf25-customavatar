// Azure Speech SDK Avatar Integration for Blazor
// This JavaScript file provides interop between Blazor and the Azure Speech SDK

window.avatarSynthesizer = null;
window.speechRecognizer = null;
window.peerConnection = null;
window.webAudioCtx = null;
window.webAudioGainNode = null;
window.sessionActive = false;

// Initialize the Azure Speech SDK
window.initializeAvatarSDK = function() {
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

    // Create peer connection
    window.peerConnection = new RTCPeerConnection({
        iceServers: [{
            urls: [iceServerUrl],
            username: username,
            credential: password
        }]
    });

    console.log('Peer connection created');

    // Ensure we have media transceivers for receiving audio/video from the service
    try {
        const videoTransceiver = window.peerConnection.addTransceiver('video', { direction: 'recvonly' });
        console.log('[WebRTC] Video transceiver added:', videoTransceiver?.mid || 'unknown mid');
    } catch (error) {
        console.error('[WebRTC] ❌ Failed to add video transceiver:', error);
        throw error;
    }

    try {
        const audioTransceiver = window.peerConnection.addTransceiver('audio', { direction: 'recvonly' });
        console.log('[WebRTC] Audio transceiver added:', audioTransceiver?.mid || 'unknown mid');
    } catch (error) {
        console.error('[WebRTC] ❌ Failed to add audio transceiver:', error);
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
    console.log('[Config] Creating speech config for region:', config.azureSpeech.region);
    const speechConfig = SpeechSDK.SpeechConfig.fromSubscription(
        config.azureSpeech.apiKey,
        config.azureSpeech.region
    );

    console.log('[Config] Speech config created successfully');

    // Configure TTS voice
    if (!config.avatar.useBuiltInVoice && config.sttTts.ttsVoice) {
        speechConfig.speechSynthesisVoiceName = config.sttTts.ttsVoice;
        console.log('[Voice] Using TTS voice:', config.sttTts.ttsVoice);
    } else {
        console.log('[Voice] Using built-in avatar voice');
    }

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
        console.log('Avatar session stopped');
    } catch (error) {
        console.error('Error stopping avatar session:', error);
    }
};

// Speak text using avatar
window.speakText = async function(text) {
    if (!window.avatarSynthesizer) {
        console.error('Avatar synthesizer not initialized');
        return;
    }

    try {
        await new Promise((resolve, reject) => {
            window.avatarSynthesizer.speakTextAsync(
                text,
                (result) => {
                    if (result.reason === SpeechSDK.ResultReason.SynthesizingAudioCompleted) {
                        console.log('Speech synthesis completed');
                        resolve();
                    } else {
                        console.error('Speech synthesis failed:', result.errorDetails);
                        reject(new Error(result.errorDetails));
                    }
                },
                (error) => {
                    console.error('Error speaking text:', error);
                    reject(error);
                }
            );
        });
    } catch (error) {
        console.error('Error in speakText:', error);
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

    // This would require additional Speech SDK configuration
    // Implementation would be similar to the JavaScript version
    console.log('Microphone functionality would be implemented here');
    alert('Microphone feature: Please use text input for now. Full microphone support requires additional Speech SDK configuration.');
};
