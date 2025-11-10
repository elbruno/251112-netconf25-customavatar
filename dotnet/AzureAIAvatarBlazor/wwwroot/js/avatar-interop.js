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

    // Handle incoming video/audio tracks
    window.peerConnection.ontrack = function(event) {
        console.log('Received track:', event.track.kind);
        
        if (event.track.kind === 'video') {
            console.log('Setting up video element...');
            let videoElement = document.createElement('video');
            videoElement.id = 'videoPlayer';
            videoElement.srcObject = event.streams[0];
            videoElement.autoplay = true;
            videoElement.playsInline = true;
            videoElement.style.width = '100%';
            videoElement.style.height = 'auto';

            const remoteVideo = document.getElementById('remoteVideo');
            if (remoteVideo) {
                // Clear existing video
                remoteVideo.innerHTML = '';
                remoteVideo.appendChild(videoElement);
                console.log('Video element added to DOM');
            } else {
                console.error('remoteVideo element not found in DOM');
            }
        } else if (event.track.kind === 'audio') {
            console.log('Setting up audio element...');
            let audioElement = document.createElement('audio');
            audioElement.id = 'audioPlayer';
            audioElement.srcObject = event.streams[0];
            audioElement.autoplay = true;

            // Setup Web Audio API for gain control
            const audioGain = config.avatar.audioGain || 1.8;
            setupAudioGain(audioElement, event.streams[0], audioGain);

            const remoteVideo = document.getElementById('remoteVideo');
            if (remoteVideo) {
                remoteVideo.appendChild(audioElement);
                console.log('Audio element added to DOM');
            }
        }
    };

    // Handle ICE connection state changes
    window.peerConnection.oniceconnectionstatechange = function() {
        console.log('ICE connection state:', window.peerConnection.iceConnectionState);
    };

    // Handle connection state changes
    window.peerConnection.onconnectionstatechange = function() {
        console.log('Connection state:', window.peerConnection.connectionState);
    };

    // Verify Speech SDK is loaded
    if (typeof SpeechSDK === 'undefined') {
        throw new Error('Speech SDK not loaded. Please refresh the page.');
    }

    // Create and initialize avatar synthesizer
    const speechConfig = SpeechSDK.SpeechConfig.fromSubscription(
        config.azureSpeech.apiKey,
        config.azureSpeech.region
    );

    console.log('Speech config created');

    // Configure TTS voice
    if (!config.avatar.useBuiltInVoice && config.sttTts.ttsVoice) {
        speechConfig.speechSynthesisVoiceName = config.sttTts.ttsVoice;
        console.log('Using TTS voice:', config.sttTts.ttsVoice);
    } else {
        console.log('Using built-in voice');
    }

    // Configure avatar - handle empty style
    const avatarStyle = config.avatar.style;
    const isCustom = config.avatar.isCustomAvatar === true;
    
    console.log('Creating avatar config with character:', config.avatar.character, 'style:', avatarStyle || '(none)', 'isCustom:', isCustom);
    
    // For custom avatars or when style is empty/null, don't pass style parameter
    let avatarConfig;
    if (!avatarStyle || avatarStyle === '') {
        console.log('Creating avatar without style parameter');
        avatarConfig = new SpeechSDK.AvatarConfig(config.avatar.character);
    } else {
        console.log('Creating avatar with style parameter:', avatarStyle);
        avatarConfig = new SpeechSDK.AvatarConfig(config.avatar.character, avatarStyle);
    }
    
    // Set customized flag for custom avatars
    avatarConfig.customized = isCustom;
    console.log('Avatar customized flag:', avatarConfig.customized);

    const videoFormat = new SpeechSDK.AvatarVideoFormat();
    videoFormat.bitrate = 2000000;

    console.log('Creating avatar synthesizer...');
    window.avatarSynthesizer = new SpeechSDK.AvatarSynthesizer(
        speechConfig,
        avatarConfig
    );

    window.avatarSynthesizer.avatarEventReceived = function(s, e) {
        console.log('[Avatar Event]:', e.description);
    };

    // Start avatar connection
    console.log('Starting avatar...');
    await new Promise((resolve, reject) => {
        window.avatarSynthesizer.startAvatarAsync(
            window.peerConnection,
            () => {
                console.log('Avatar started successfully!');
                resolve();
            },
            (error) => {
                console.error('Error starting avatar:', error);
                reject(new Error('Failed to start avatar: ' + error));
            }
        );
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
