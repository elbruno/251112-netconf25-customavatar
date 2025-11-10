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

// Start avatar session
window.startAvatarSession = async function(config) {
    console.log('Starting avatar session with config:', config);
    
    try {
        // Get Speech SDK token
        const region = config.azureSpeech.region;
        const subscriptionKey = config.azureSpeech.apiKey;
        
        if (!subscriptionKey || !region) {
            alert('Please configure Azure Speech credentials in the Configuration page.');
            return;
        }

        // Request avatar relay token
        const tokenUrl = `https://${region}.tts.speech.microsoft.com/cognitiveservices/avatar/relay/token/v1`;
        
        const response = await fetch(tokenUrl, {
            method: 'GET',
            headers: {
                'Ocp-Apim-Subscription-Key': subscriptionKey
            }
        });

        if (!response.ok) {
            throw new Error(`Failed to get avatar token: ${response.status}`);
        }

        const tokenData = await response.json();
        console.log('Avatar token retrieved successfully');

        // Setup WebRTC connection
        await setupWebRTC(tokenData.Urls[0], tokenData.Username, tokenData.Password, config);
        
        window.sessionActive = true;
        console.log('Avatar session started successfully');
    } catch (error) {
        console.error('Error starting avatar session:', error);
        alert('Failed to start avatar session: ' + error.message);
    }
};

// Setup WebRTC connection
async function setupWebRTC(iceServerUrl, username, password, config) {
    // Create peer connection
    window.peerConnection = new RTCPeerConnection({
        iceServers: [{
            urls: [iceServerUrl],
            username: username,
            credential: password
        }]
    });

    // Handle incoming video/audio tracks
    window.peerConnection.ontrack = function(event) {
        console.log('Received track:', event.track.kind);
        
        if (event.track.kind === 'video') {
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
            }
        } else if (event.track.kind === 'audio') {
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
            }
        }
    };

    // Create and initialize avatar synthesizer
    const speechConfig = SpeechSDK.SpeechConfig.fromSubscription(
        config.azureSpeech.apiKey,
        config.azureSpeech.region
    );

    // Configure TTS voice
    if (!config.avatar.useBuiltInVoice) {
        speechConfig.speechSynthesisVoiceName = config.sttTts.ttsVoice;
    }

    // Configure avatar
    const avatarConfig = new SpeechSDK.AvatarConfig(
        config.avatar.character,
        config.avatar.style
    );
    
    avatarConfig.customized = config.avatar.isCustomAvatar;

    const videoFormat = new SpeechSDK.AvatarVideoFormat();
    videoFormat.bitrate = 2000000;

    window.avatarSynthesizer = new SpeechSDK.AvatarSynthesizer(
        speechConfig,
        avatarConfig
    );

    window.avatarSynthesizer.avatarEventReceived = function(s, e) {
        console.log('Avatar event:', e.description);
    };

    // Start avatar connection
    await new Promise((resolve, reject) => {
        window.avatarSynthesizer.startAvatarAsync(
            window.peerConnection,
            () => {
                console.log('Avatar started successfully');
                resolve();
            },
            (error) => {
                console.error('Error starting avatar:', error);
                reject(error);
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
