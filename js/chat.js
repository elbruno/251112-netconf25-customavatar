// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.

// Global objects
var speechRecognizer
var avatarSynthesizer
var peerConnection
var peerConnectionDataChannel
var messages = []
var messageInitiated = false
var dataSources = []
var sentenceLevelPunctuations = [ '.', '?', '!', ':', ';', '。', '？', '！', '：', '；' ]
var enableDisplayTextAlignmentWithSpeech = true
var enableQuickReply = false
var quickReplies = [ 'Let me take a look.', 'Let me check.', 'One moment, please.' ]
var byodDocRegex = new RegExp(/\[doc(\d+)\]/g)
var isSpeaking = false
var isReconnecting = false
var speakingText = ""
var spokenTextQueue = []
var repeatSpeakingSentenceAfterReconnection = true
var sessionActive = false
var userClosedSession = false
var lastInteractionTime = new Date()
var lastSpeakTime
var imgUrl = ""
var isStarting = false
var webAudioCtx
var webAudioGainNode
var isMicListening = false

// Mic overlay helpers
function updateMicOverlayUI() {
    try {
        const overlay = document.getElementById('micOverlay')
        const label = document.getElementById('micOverlayLabel')
        if (!overlay || !label) return
        // Show overlay when remote video is playing and mic is not listening
        const video = document.getElementById('videoPlayer')
        const videoReady = !!video && !video.paused && !video.ended
        if (videoReady && !isMicListening) {
            label.textContent = 'Press the mic to start talking'
            overlay.hidden = false
        } else if (videoReady && isMicListening) {
            label.textContent = 'Listening…'
            overlay.hidden = true
        } else {
            overlay.hidden = true
        }
    } catch {}
}
function hideMicOverlay() {
    const overlay = document.getElementById('micOverlay')
    if (overlay) overlay.hidden = true
}

// Simple wrapper function for button click
async function startAvatarSession() {
    console.log('Start Avatar Session clicked');
    try {
        if (window.startSession) {
            await window.startSession();
        } else {
            alert('Error: startSession function not available.');
        }
    } catch (error) {
        console.error('Error starting avatar session:', error);
        alert('Error starting avatar session: ' + error.message);
    }
}

window.debugAvatarConnection = function() {
    const region = getConfigValue('region')
    const speechKey = getConfigValue('APIKey')
    const aoaiEp = getConfigValue('azureOpenAIEndpoint')
    const aoaiKey = getConfigValue('azureOpenAIApiKey')
    const aoaiDep = getConfigValue('azureOpenAIDeploymentName')
    const enablePE = getBooleanConfig('enablePrivateEndpoint')
    const pe = getConfigValue('privateEndpoint')
    const msgs = []
    if (!speechKey) msgs.push('Missing Speech API key')
    if (!region && !enablePE) msgs.push('Missing Speech region (or enable Private Endpoint)')
    if (enablePE && !pe) msgs.push('Private Endpoint enabled but endpoint URL missing')
    if (!aoaiEp) msgs.push('Missing Azure OpenAI endpoint')
    if (!aoaiKey) msgs.push('Missing Azure OpenAI API key')
    if (!aoaiDep) msgs.push('Missing Azure OpenAI deployment name')
    console.log('Avatar preflight:', { region, speechKeyPresent: !!speechKey, aoaiEp, aoaiKeyPresent: !!aoaiKey, aoaiDep, enablePE, pe })
    alert(msgs.length ? ('Issues found:\n- ' + msgs.join('\n- ')) : 'Avatar connection prerequisites look OK.')
}

// Helper function to get configuration value with fallback
function getConfigValue(elementId, fallbackValue = '') {
    // First try to get from configuration manager
    if (window.configManager) {
        const config = window.configManager.getConfiguration()
        if (config[elementId] !== undefined && config[elementId] !== '') {
            return config[elementId];
        }
    }
    
    // Then try to get from DOM element (if it exists)
    const element = document.getElementById(elementId)
    if (element) {
        // Prefer checkbox.checked when applicable
        if (element.type === 'checkbox') {
            return !!element.checked
        }
        return element.value || fallbackValue
    }
    
    return fallbackValue
}

// Helper to reliably get a boolean from config or DOM
function getBooleanConfig(key, fallback = false) {
    // Prefer DOM checkbox when available
    const el = document.getElementById(key)
    if (el && el.type === 'checkbox') {
        return !!el.checked
    }
    // Fallback to configuration manager
    if (window.configManager) {
        const cfg = window.configManager.getConfiguration()
        if (typeof cfg[key] === 'boolean') return cfg[key]
        if (typeof cfg[key] === 'string') {
            const v = cfg[key].toLowerCase();
            if (v === 'true') return true
            if (v === 'false') return false
        }
    }
    return fallback
}

// Connect to avatar service
async function connectAvatar() {
    if (isStarting) {
        console.log('Start ignored: another start is in progress.')
        return
    }
    isStarting = true

    // Proactively tear down any existing connections to avoid concurrency limits
    try { disconnectAvatar(true) } catch {}
    const cogSvcRegion = getConfigValue('region')
    const cogSvcSubKey = getConfigValue('APIKey')
    
    console.log('Connecting avatar with region:', cogSvcRegion);
    console.log('API Key present:', !!cogSvcSubKey);
    
    if (cogSvcSubKey === '') {
        alert('Please configure your API key first. Go to Configuration page.')
    isStarting = false
    return
    }

    const privateEndpointEnabled = getBooleanConfig('enablePrivateEndpoint')
    const privateEndpoint = getConfigValue('privateEndpoint')
    if (privateEndpointEnabled && privateEndpoint === '') {
        alert('Please configure the Azure Speech endpoint first.')
    isStarting = false
    return
    }

    let speechSynthesisConfig
    if (privateEndpointEnabled) {
        speechSynthesisConfig = SpeechSDK.SpeechConfig.fromEndpoint(new URL(`wss://${privateEndpoint.slice(8)}/tts/cognitiveservices/websocket/v1?enableTalkingAvatar=true`), cogSvcSubKey) 
    } else {
        speechSynthesisConfig = SpeechSDK.SpeechConfig.fromSubscription(cogSvcSubKey, cogSvcRegion)
    }
    const useBuiltIn = getBooleanConfig('useBuiltInVoice')
    let customEndpointId = getConfigValue('customVoiceEndpointId') || ''
    // Sanitize obvious placeholders
    const idTrim = String(customEndpointId).trim().toLowerCase()
    const looksPlaceholder = !idTrim || idTrim === 'your_custom_voice_endpoint_id' || idTrim.startsWith('xxxxx')
    // Si se usa la voz incorporada del avatar, o el id parece placeholder, no usar endpoint de Custom Voice
    speechSynthesisConfig.endpointId = (useBuiltIn || looksPlaceholder) ? '' : customEndpointId
    console.log('TTS routing:', { useBuiltInVoice: useBuiltIn, endpointIdUsed: speechSynthesisConfig.endpointId ? 'custom-endpoint' : 'standard' })

    const isCustomAvatar = getBooleanConfig('customizedAvatar')
    let talkingAvatarCharacter = getConfigValue('talkingAvatarCharacter') || 'lisa'
    let talkingAvatarStyle = getConfigValue('talkingAvatarStyle')
    // Ajuste de mayúsculas para avatares personalizados conocidos
    if (isCustomAvatar && talkingAvatarCharacter) {
        const lcName = talkingAvatarCharacter.toLowerCase()
        if (lcName === 'pablitopiova') talkingAvatarCharacter = 'PablitoPiova'
        if (lcName === 'pablitopiovaagentcon') talkingAvatarCharacter = 'PablitoPiovaAgentCon'
    }
    // Para avatares no personalizados, mantener fallback por compatibilidad
    if (!isCustomAvatar && (!talkingAvatarStyle || talkingAvatarStyle === '')) {
        talkingAvatarStyle = 'casual-sitting'
    }
    // Si el estilo es vacío en custom, pasar solo character
    const avatarConfig = (talkingAvatarStyle && talkingAvatarStyle !== '')
        ? new SpeechSDK.AvatarConfig(talkingAvatarCharacter, talkingAvatarStyle)
        : new SpeechSDK.AvatarConfig(talkingAvatarCharacter)
    avatarConfig.customized = isCustomAvatar
    avatarConfig.useBuiltInVoice = useBuiltIn
    avatarConfig.useBuiltInVoice = getBooleanConfig('useBuiltInVoice')
    avatarSynthesizer = new SpeechSDK.AvatarSynthesizer(speechSynthesisConfig, avatarConfig)
    avatarSynthesizer.avatarEventReceived = function (s, e) {
        var offsetMessage = ", offset from session start: " + e.offset / 10000 + "ms."
        if (e.offset === 0) {
            offsetMessage = ""
        }

        console.log("Event received: " + e.description + offsetMessage)
    }

    let speechRecognitionConfig
    if (privateEndpointEnabled) {
        speechRecognitionConfig = SpeechSDK.SpeechConfig.fromEndpoint(new URL(`wss://${privateEndpoint}/stt/speech/universal/v2`), cogSvcSubKey) 
    } else {
        speechRecognitionConfig = SpeechSDK.SpeechConfig.fromEndpoint(new URL(`wss://${cogSvcRegion}.stt.speech.microsoft.com/speech/universal/v2`), cogSvcSubKey)
    }
    speechRecognitionConfig.setProperty(SpeechSDK.PropertyId.SpeechServiceConnection_LanguageIdMode, "Continuous")
    var sttLocales = String(getConfigValue('sttLocales', 'en-US')).split(',')
    var autoDetectSourceLanguageConfig = SpeechSDK.AutoDetectSourceLanguageConfig.fromLanguages(sttLocales)
    speechRecognizer = SpeechSDK.SpeechRecognizer.FromConfig(speechRecognitionConfig, autoDetectSourceLanguageConfig, SpeechSDK.AudioConfig.fromDefaultMicrophoneInput())

    const azureOpenAIEndpoint = getConfigValue('azureOpenAIEndpoint')
    const azureOpenAIApiKey = getConfigValue('azureOpenAIApiKey')
    const azureOpenAIDeploymentName = getConfigValue('azureOpenAIDeploymentName')
    if (azureOpenAIEndpoint === '' || azureOpenAIApiKey === '' || azureOpenAIDeploymentName === '') {
        alert('Please fill in the Azure OpenAI endpoint, API key and deployment name.')
        isStarting = false
        return
    }

    dataSources = []
    if (getBooleanConfig('enableOyd')) {
        const azureCogSearchEndpoint = getConfigValue('azureCogSearchEndpoint')
        const azureCogSearchApiKey = getConfigValue('azureCogSearchApiKey')
        const azureCogSearchIndexName = getConfigValue('azureCogSearchIndexName')
        if (azureCogSearchEndpoint === "" || azureCogSearchApiKey === "" || azureCogSearchIndexName === "") {
            alert('Please fill in the Azure Cognitive Search endpoint, API key and index name.')
            return
        } else {
            setDataSources(azureCogSearchEndpoint, azureCogSearchApiKey, azureCogSearchIndexName)
        }
    }

    // Only initialize messages once (ensure profiles are applied first)
    if (!messageInitiated) {
        await initMessages()
        messageInitiated = true
    }

    const startBtn = document.getElementById('startSession')
    if (startBtn) startBtn.disabled = true
    const configurationDiv = document.getElementById('configuration')
    if (configurationDiv) configurationDiv.hidden = true

    const xhr = new XMLHttpRequest()
    if (privateEndpointEnabled) {
        xhr.open("GET", `https://${privateEndpoint}/tts/cognitiveservices/avatar/relay/token/v1`)
    } else {
        xhr.open("GET", `https://${cogSvcRegion}.tts.speech.microsoft.com/cognitiveservices/avatar/relay/token/v1`)
    }
    xhr.setRequestHeader("Ocp-Apim-Subscription-Key", cogSvcSubKey)
    xhr.addEventListener("readystatechange", function() {
        if (this.readyState === 4) {
            try {
                if (this.status !== 200) {
                    console.error('Token request failed:', this.status, this.responseText)
                    alert('Failed to get avatar token. Please verify Speech region/key and try again.')
                    isStarting = false
                    const startBtn = document.getElementById('startSession')
                    if (startBtn) startBtn.disabled = false
                    const configurationDiv = document.getElementById('configuration')
                    if (configurationDiv) configurationDiv.hidden = false
                    return
                }
                const responseData = JSON.parse(this.responseText)
                const iceServerUrl = responseData.Urls[0]
                const iceServerUsername = responseData.Username
                const iceServerCredential = responseData.Password
                setupWebRTC(iceServerUrl, iceServerUsername, iceServerCredential)
            } catch (e) {
                console.error('Token response parsing failed:', e)
                alert('Unexpected token response from service. Check console for details.')
                isStarting = false
                const startBtn = document.getElementById('startSession')
                if (startBtn) startBtn.disabled = false
                const configurationDiv = document.getElementById('configuration')
                if (configurationDiv) configurationDiv.hidden = false
            }
        }
    })
    xhr.send()
}

// Disconnect from avatar service
function disconnectAvatar(fromStart = false) {
    if (avatarSynthesizer !== undefined) {
        avatarSynthesizer.close()
    }

    if (speechRecognizer !== undefined) {
        speechRecognizer.stopContinuousRecognitionAsync()
        speechRecognizer.close()
    }

    try {
        if (peerConnection) {
            peerConnection.ontrack = null
            peerConnection.oniceconnectionstatechange = null
            peerConnectionDataChannel = null
            peerConnection.close()
        }
    } catch {}
    peerConnection = undefined
    peerConnectionDataChannel = undefined

    // Clean media elements
    try {
        const remote = document.getElementById('remoteVideo')
        if (remote) remote.innerHTML = ''
    } catch {}

    // Teardown WebAudio
    try {
        if (webAudioGainNode) {
            webAudioGainNode.disconnect()
            webAudioGainNode = undefined
        }
        if (webAudioCtx && webAudioCtx.state !== 'closed') {
            webAudioCtx.close().catch(() => {})
        }
    } catch {}
    webAudioCtx = undefined

    sessionActive = false
    isMicListening = false
    hideMicOverlay()
    if (!fromStart) {
        isStarting = false
    }
}

// Setup WebRTC
function setupWebRTC(iceServerUrl, iceServerUsername, iceServerCredential) {
    // Create WebRTC peer connection
    peerConnection = new RTCPeerConnection({
        iceServers: [{
            urls: [ iceServerUrl ],
            username: iceServerUsername,
            credential: iceServerCredential
        }]
    })

    // Fetch WebRTC video stream and mount it to an HTML video element
    peerConnection.ontrack = function (event) {
    if (event.track.kind === 'audio') {
            let audioElement = document.createElement('audio')
            audioElement.id = 'audioPlayer'
            audioElement.srcObject = event.streams[0]
            audioElement.autoplay = false
            audioElement.addEventListener('loadeddata', () => {
                audioElement.play()
            })

            audioElement.onplaying = () => {
                console.log(`WebRTC ${event.track.kind} channel connected.`)
            }

            // Clean up existing audio element if there is any
            remoteVideoDiv = document.getElementById('remoteVideo')
            for (var i = 0; i < remoteVideoDiv.childNodes.length; i++) {
                if (remoteVideoDiv.childNodes[i].localName === event.track.kind) {
                    remoteVideoDiv.removeChild(remoteVideoDiv.childNodes[i])
                }
            }

            // WebAudio boost pipeline
            try {
                const desiredGain = Number(localStorage.getItem('azureAIFoundryAudioGain') || '1.8')
                if (!webAudioCtx) {
                    const Ctx = window.AudioContext || window.webkitAudioContext
                    if (Ctx) webAudioCtx = new Ctx()
                }
                if (webAudioCtx) {
                    // Mute element to avoid double playback and route through WebAudio instead
                    audioElement.muted = true
                    const source = webAudioCtx.createMediaStreamSource(event.streams[0])
                    webAudioGainNode = webAudioCtx.createGain()
                    webAudioGainNode.gain.value = Math.max(0.1, Math.min(isFinite(desiredGain) ? desiredGain : 1.8, 5.0))
                    source.connect(webAudioGainNode).connect(webAudioCtx.destination)
                    console.log('Audio gain boost active:', webAudioGainNode.gain.value)
                } else {
                    // Fallback: use element volume up to 1.0
                    audioElement.volume = 1.0
                }
            } catch (e) {
                console.log('WebAudio gain setup failed, falling back to element audio:', e?.message)
                audioElement.volume = 1.0
            }

            // Append the new audio element
            document.getElementById('remoteVideo').appendChild(audioElement)
        }

        if (event.track.kind === 'video') {
            let videoElement = document.createElement('video')
            videoElement.id = 'videoPlayer'
            videoElement.srcObject = event.streams[0]
            videoElement.autoplay = false
            videoElement.addEventListener('loadeddata', () => {
                videoElement.play()
            })

            videoElement.playsInline = true
            videoElement.style.width = '0.5px'
            document.getElementById('remoteVideo').appendChild(videoElement)

            // Continue speaking if there are unfinished sentences
            if (repeatSpeakingSentenceAfterReconnection) {
                if (speakingText !== '') {
                    speakNext(speakingText, 0, true)
                }
            } else {
                if (spokenTextQueue.length > 0) {
                    speakNext(spokenTextQueue.shift())
                }
            }

            videoElement.onplaying = () => {
                // Clean up existing video element if there is any
                remoteVideoDiv = document.getElementById('remoteVideo')
                for (var i = 0; i < remoteVideoDiv.childNodes.length; i++) {
                    if (remoteVideoDiv.childNodes[i].localName === event.track.kind) {
                        remoteVideoDiv.removeChild(remoteVideoDiv.childNodes[i])
                    }
                }

                // Append the new video element responsively
                videoElement.style.width = '100%'
                document.getElementById('remoteVideo').appendChild(videoElement)

                console.log(`WebRTC ${event.track.kind} channel connected.`)
                document.getElementById('microphone').disabled = false
                document.getElementById('stopSession').disabled = false
                document.getElementById('remoteVideo').style.width = '100%'
                document.getElementById('chatHistory').hidden = false
                document.getElementById('showTypeMessage').disabled = false
                // Update mic overlay when video is ready
                setTimeout(updateMicOverlayUI, 150)
                // Forzar play del canal de audio si está pausado por políticas de autoplay
                const audioEl = document.getElementById('audioPlayer')
                if (audioEl && audioEl.paused) {
                    audioEl.play().catch(() => {})
                }
                // Reanudar contexto WebAudio si está suspendido
                if (webAudioCtx && webAudioCtx.state === 'suspended') {
                    webAudioCtx.resume().catch(() => {})
                }

                if (getBooleanConfig('useLocalVideoForIdle')) {
                    const lv = document.getElementById('localVideo')
                    if (lv) lv.hidden = true
                    if (lastSpeakTime === undefined) {
                        lastSpeakTime = new Date()
                    }
                }

                isReconnecting = false
                setTimeout(() => { sessionActive = true }, 5000) // Set session active after 5 seconds
            }
        }
    }
    
     // Listen to data channel, to get the event from the server
    peerConnection.addEventListener("datachannel", event => {
        peerConnectionDataChannel = event.channel
        peerConnectionDataChannel.onmessage = e => {
            let subtitles = document.getElementById('subtitles')
            const webRTCEvent = JSON.parse(e.data)
        const showSubs = getBooleanConfig('showSubtitles')
        if (webRTCEvent.event.eventType === 'EVENT_TYPE_TURN_START' && showSubs) {
                subtitles.hidden = false
                subtitles.innerHTML = speakingText
            } else if (webRTCEvent.event.eventType === 'EVENT_TYPE_SESSION_END' || webRTCEvent.event.eventType === 'EVENT_TYPE_SWITCH_TO_IDLE') {
                subtitles.hidden = true
                if (webRTCEvent.event.eventType === 'EVENT_TYPE_SESSION_END') {
            if (getBooleanConfig('autoReconnectAvatar') && !userClosedSession && !isReconnecting) {
                        // No longer reconnect when there is no interaction for a while
                        if (new Date() - lastInteractionTime < 300000) {
                            // Session disconnected unexpectedly, need reconnect
                            console.log(`[${(new Date()).toISOString()}] The WebSockets got disconnected, need reconnect.`)
                            isReconnecting = true

                            // Remove data channel onmessage callback to avoid duplicatedly triggering reconnect
                            peerConnectionDataChannel.onmessage = null

                            // Release the existing avatar connection
                            if (avatarSynthesizer !== undefined) {
                                avatarSynthesizer.close()
                            }

                            // Setup a new avatar connection
                            connectAvatar()
                        }
                    }
                }
            }

            console.log("[" + (new Date()).toISOString() + "] WebRTC event received: " + e.data)
        }
    })

    // This is a workaround to make sure the data channel listening is working by creating a data channel from the client side
    c = peerConnection.createDataChannel("eventChannel")

    // Make necessary update to the web page when the connection state changes
    peerConnection.oniceconnectionstatechange = e => {
        console.log("WebRTC status: " + peerConnection.iceConnectionState)
        if (peerConnection.iceConnectionState === 'disconnected') {
            if (getBooleanConfig('useLocalVideoForIdle')) {
                const lv = document.getElementById('localVideo')
                if (lv) lv.hidden = false
                document.getElementById('remoteVideo').style.width = '0.1px'
            }
        }
    }

    // Offer to receive 1 audio, and 1 video track
    peerConnection.addTransceiver('video', { direction: 'sendrecv' })
    peerConnection.addTransceiver('audio', { direction: 'sendrecv' })

    // start avatar, establish WebRTC connection
    avatarSynthesizer.startAvatarAsync(peerConnection).then((r) => {
        if (r.reason === SpeechSDK.ResultReason.SynthesizingAudioCompleted) {
            console.log("[" + (new Date()).toISOString() + "] Avatar started. Result ID: " + r.resultId)
            isStarting = false
            setTimeout(updateMicOverlayUI, 250)
        } else {
            console.log("[" + (new Date()).toISOString() + "] Unable to start avatar. Result ID: " + r.resultId)
            if (r.reason === SpeechSDK.ResultReason.Canceled) {
                let cancellationDetails = SpeechSDK.CancellationDetails.fromResult(r)
                if (cancellationDetails.reason === SpeechSDK.CancellationReason.Error) {
                    console.log(cancellationDetails.errorDetails)
                };

                console.log("Unable to start avatar: " + cancellationDetails.errorDetails);
            }
            document.getElementById('startSession').disabled = false;
            const cfg = document.getElementById('configuration');
            if (cfg) cfg.hidden = false;
            isStarting = false
        }
    }).catch(
        (error) => {
            console.log("[" + (new Date()).toISOString() + "] Avatar failed to start. Error: " + error)
            document.getElementById('startSession').disabled = false
            const cfg = document.getElementById('configuration');
            if (cfg) cfg.hidden = false
            isStarting = false
        }
    )
}

// Initialize messages
async function initMessages() {
    messages = []
    // Always include the system prompt as the first message so the model
        // Robustness: if a prompt profile is configured but not yet applied,
        // try to apply it before reading the prompt value so the system
        // prompt reflects the selected profile.
        try {
            if (window.configManager) {
                const cfg = window.configManager.getConfiguration();
                // If a profile is requested, try to apply it (this is async)
                if (cfg && cfg.promptProfile) {
                    // Only attempt when the UI hasn't already applied it
                    try {
                        await window.configManager.applyPromptProfileIfConfigured();
                        console.log('[initMessages] Prompt profile applied (if present).');
                    } catch (e) {
                        console.warn('[initMessages] Failed to auto-apply prompt profile:', e?.message || e);
                    }
                }
            }
        } catch (e) {
            console.warn('initMessages: error while ensuring prompt profile applied', e?.message || e);
        }

        // Always include the system prompt as the first message so the model
        // consistently receives the configured system context.
        let systemPrompt = getConfigValue('prompt', '')
        console.log('[initMessages] Injecting system prompt (first 200 chars):', String(systemPrompt || '').substring(0,200))
        let systemMessage = {
            role: 'system',
            content: systemPrompt
        }

        messages.push(systemMessage)
}

// Set data sources for chat API
function setDataSources(azureCogSearchEndpoint, azureCogSearchApiKey, azureCogSearchIndexName) {
    let dataSource = {
        type: 'AzureCognitiveSearch',
        parameters: {
            endpoint: azureCogSearchEndpoint,
            key: azureCogSearchApiKey,
            indexName: azureCogSearchIndexName,
            semanticConfiguration: '',
            queryType: 'simple',
            fieldsMapping: {
                contentFieldsSeparator: '\n',
                contentFields: ['content'],
                filepathField: null,
                titleField: 'title',
                urlField: null
            },
            inScope: true,
            roleInformation: getConfigValue('prompt', '')
        }
    }

    dataSources.push(dataSource)
}

// Do HTML encoding on given text
function htmlEncode(text) {
    const entityMap = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#39;',
      '/': '&#x2F;'
    };

    return String(text).replace(/[&<>"'\/]/g, (match) => entityMap[match])
}

// Speak the given text
function speak(text, endingSilenceMs = 0) {
    if (isSpeaking) {
        spokenTextQueue.push(text)
        return
    }

    speakNext(text, endingSilenceMs)
}

function speakNext(text, endingSilenceMs = 0, skipUpdatingChatHistory = false) {
    const useBuiltIn = getBooleanConfig('useBuiltInVoice')
    let ttsVoice = getConfigValue('ttsVoice', 'en-US-AvaMultilingualNeural')
    // Si se usa la voz incorporada del avatar, no forzar voice name en SSML
    let ssml = useBuiltIn
        ? `<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'><mstts:leadingsilence-exact value='0'/>${htmlEncode(text)}</speak>`
        : `<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'><voice name='${ttsVoice}'><mstts:leadingsilence-exact value='0'/>${htmlEncode(text)}</voice></speak>`
    if (endingSilenceMs > 0) {
        ssml = useBuiltIn
            ? `<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'><mstts:leadingsilence-exact value='0'/>${htmlEncode(text)}<break time='${endingSilenceMs}ms' /></speak>`
            : `<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'><voice name='${ttsVoice}'><mstts:leadingsilence-exact value='0'/>${htmlEncode(text)}<break time='${endingSilenceMs}ms' /></voice></speak>`
    }

    if (enableDisplayTextAlignmentWithSpeech && !skipUpdatingChatHistory) {
        let chatHistoryTextArea = document.getElementById('chatHistory')
        chatHistoryTextArea.innerHTML += text.replace(/\n/g, '<br/>')
        chatHistoryTextArea.scrollTop = chatHistoryTextArea.scrollHeight
    }

    lastSpeakTime = new Date()
    isSpeaking = true
    speakingText = text
    document.getElementById('stopSpeaking').disabled = false
    const speakPromise = useBuiltIn
        ? avatarSynthesizer.speakTextAsync(text)
        : avatarSynthesizer.speakSsmlAsync(ssml)
    speakPromise.then(
        (result) => {
            if (result.reason === SpeechSDK.ResultReason.SynthesizingAudioCompleted) {
                console.log(`Speech synthesized to speaker for text [ ${text} ]. Result ID: ${result.resultId}`)
                lastSpeakTime = new Date()
            } else {
                console.log(`Error occurred while speaking the SSML. Result ID: ${result.resultId}`)
            }

            speakingText = ''

            if (spokenTextQueue.length > 0) {
                speakNext(spokenTextQueue.shift())
            } else {
                isSpeaking = false
                document.getElementById('stopSpeaking').disabled = true
            }
    }).catch(
            (error) => {
                console.log(`Error occurred while speaking the SSML: [ ${error} ]`)

                speakingText = ''

                if (spokenTextQueue.length > 0) {
                    speakNext(spokenTextQueue.shift())
                } else {
                    isSpeaking = false
                    document.getElementById('stopSpeaking').disabled = true
                }
            }
        )
}

function stopSpeaking() {
    lastInteractionTime = new Date()
    spokenTextQueue = []
    avatarSynthesizer.stopSpeakingAsync().then(
        () => {
            isSpeaking = false
            document.getElementById('stopSpeaking').disabled = true
            console.log("[" + (new Date()).toISOString() + "] Stop speaking request sent.")
        }
    ).catch(
        (error) => {
            console.log("Error occurred while stopping speaking: " + error)
        }
    )
}

function handleUserQuery(userQuery) {
    lastInteractionTime = new Date()
    let contentMessage = userQuery
    let chatMessage = {
        role: 'user',
        content: contentMessage
    }

    messages.push(chatMessage)
    let chatHistoryTextArea = document.getElementById('chatHistory')
    if (chatHistoryTextArea.innerHTML !== '' && !chatHistoryTextArea.innerHTML.endsWith('\n\n')) {
        chatHistoryTextArea.innerHTML += '\n\n'
    }

    chatHistoryTextArea.innerHTML += "<br/><br/>User: " + userQuery + "<br/>";
        
    chatHistoryTextArea.scrollTop = chatHistoryTextArea.scrollHeight

    // Stop previous speaking if there is any
    if (isSpeaking) {
        stopSpeaking()
    }

    // For 'bring your data' scenario, chat API currently has long (4s+) latency
    // We return some quick reply here before the chat API returns to mitigate.
    if (dataSources.length > 0 && enableQuickReply) {
        speak(getQuickReply(), 2000)
    }

    const azureOpenAIEndpoint = getConfigValue('azureOpenAIEndpoint')
    const azureOpenAIApiKey = getConfigValue('azureOpenAIApiKey')
    const azureOpenAIDeploymentName = getConfigValue('azureOpenAIDeploymentName')

    let url = "{AOAIEndpoint}/openai/deployments/{AOAIDeployment}/chat/completions?api-version=2023-06-01-preview".replace("{AOAIEndpoint}", azureOpenAIEndpoint).replace("{AOAIDeployment}", azureOpenAIDeploymentName)
    let body = JSON.stringify({
        messages: messages,
        stream: true
    })

    if (dataSources.length > 0) {
        url = "{AOAIEndpoint}/openai/deployments/{AOAIDeployment}/extensions/chat/completions?api-version=2023-06-01-preview".replace("{AOAIEndpoint}", azureOpenAIEndpoint).replace("{AOAIDeployment}", azureOpenAIDeploymentName)
        body = JSON.stringify({
            dataSources: dataSources,
            messages: messages,
            stream: true
        })
    }

    let assistantReply = ''
    let toolContent = ''
    let spokenSentence = ''
    let displaySentence = ''

    fetch(url, {
        method: 'POST',
        headers: {
            'api-key': azureOpenAIApiKey,
            'Content-Type': 'application/json'
        },
        body: body
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`Chat API response status: ${response.status} ${response.statusText}`)
        }

    let chatHistoryTextArea = document.getElementById('chatHistory')
    chatHistoryTextArea.innerHTML += '<br/>Assistant: '

        const reader = response.body.getReader()

        // Function to recursively read chunks from the stream
        function read(previousChunkString = '') {
            return reader.read().then(({ value, done }) => {
                // Check if there is still data to read
                if (done) {
                    // Stream complete
                    return
                }

                // Process the chunk of data (value)
                let chunkString = new TextDecoder().decode(value, { stream: true })
                if (previousChunkString !== '') {
                    // Concatenate the previous chunk string in case it is incomplete
                    chunkString = previousChunkString + chunkString
                }

                if (!chunkString.endsWith('}\n\n') && !chunkString.endsWith('[DONE]\n\n')) {
                    // This is a incomplete chunk, read the next chunk
                    return read(chunkString)
                }

                chunkString.split('\n\n').forEach((line) => {
                    try {
                        if (line.startsWith('data:') && !line.endsWith('[DONE]')) {
                            const payload = line.substring(5).trim()
                            if (!payload) return
                            const responseJson = JSON.parse(payload)
                            let responseToken = undefined
                            if (!responseJson.choices || responseJson.choices.length === 0) {
                                // Some chunks can be empty; skip
                                return
                            }
                            if (dataSources.length === 0) {
                                const delta = responseJson.choices[0].delta || {}
                                responseToken = delta.content
                            } else {
                                const choice = responseJson.choices[0]
                                const msg = choice.messages && choice.messages[0]
                                const delta = msg && msg.delta ? msg.delta : {}
                                const role = delta.role
                                if (role === 'tool') {
                                    toolContent = delta.content || ''
                                } else {
                                    responseToken = delta.content
                                    if (responseToken !== undefined) {
                                        if (byodDocRegex.test(responseToken)) {
                                            responseToken = responseToken.replace(byodDocRegex, '').trim()
                                        }
                                        if (responseToken === '[DONE]') {
                                            responseToken = undefined
                                        }
                                    }
                                }
                            }

                            if (responseToken !== undefined && responseToken !== null) {
                                assistantReply += responseToken // build up the assistant message
                                displaySentence += responseToken // build up the display sentence

                                // console.log(`Current token: ${responseToken}`)

                                if (responseToken === '\n' || responseToken === '\n\n') {
                                    spokenSentence += responseToken
                                    speak(spokenSentence)
                                    spokenSentence = ''
                                } else {
                                    spokenSentence += responseToken // build up the spoken sentence

                                    responseToken = responseToken.replace(/\n/g, '')
                                    if (responseToken.length === 1 || responseToken.length === 2) {
                                        for (let i = 0; i < sentenceLevelPunctuations.length; ++i) {
                                            let sentenceLevelPunctuation = sentenceLevelPunctuations[i]
                                            if (responseToken.startsWith(sentenceLevelPunctuation)) {
                                                speak(spokenSentence)
                                                spokenSentence = ''
                                                break
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    } catch (error) {
                        console.log(`Error occurred while parsing the response: ${error}`)
                        console.log(chunkString)
                    }
                })

                if (!enableDisplayTextAlignmentWithSpeech) {
                    chatHistoryTextArea.innerHTML += displaySentence.replace(/\n/g, '<br/>')
                    chatHistoryTextArea.scrollTop = chatHistoryTextArea.scrollHeight
                    displaySentence = ''
                }

                // Continue reading the next chunk
                return read()
            })
        }

        // Start reading the stream
        return read()
    })
    .then(() => {
        if (spokenSentence !== '') {
            speak(spokenSentence)
            spokenSentence = ''
        }

        if (dataSources.length > 0) {
            let toolMessage = {
                role: 'tool',
                content: toolContent
            }

            messages.push(toolMessage)
        }

        let assistantMessage = {
            role: 'assistant',
            content: assistantReply
        }

        messages.push(assistantMessage)
    })
}

function getQuickReply() {
    return quickReplies[Math.floor(Math.random() * quickReplies.length)]
}

function checkHung() {
    // Check whether the avatar video stream is hung, by checking whether the video time is advancing
    let videoElement = document.getElementById('videoPlayer')
    if (videoElement !== null && videoElement !== undefined && sessionActive) {
        let videoTime = videoElement.currentTime
        setTimeout(() => {
            // Check whether the video time is advancing
            if (videoElement.currentTime === videoTime) {
                // Check whether the session is active to avoid duplicatedly triggering reconnect
                if (sessionActive) {
                    sessionActive = false
                    if (getBooleanConfig('autoReconnectAvatar')) {
                        // No longer reconnect when there is no interaction for a while
                        if (new Date() - lastInteractionTime < 300000) {
                            console.log(`[${(new Date()).toISOString()}] The video stream got disconnected, need reconnect.`)
                            isReconnecting = true
                            // Remove data channel onmessage callback to avoid duplicatedly triggering reconnect
                            peerConnectionDataChannel.onmessage = null
                            // Release the existing avatar connection
                            if (avatarSynthesizer !== undefined) {
                                avatarSynthesizer.close()
                            }
    
                            // Setup a new avatar connection
                            connectAvatar()
                        }
                    }
                }
            }
        }, 2000)
    }
}

function checkLastSpeak() {
    if (lastSpeakTime === undefined) {
        return
    }

    let currentTime = new Date()
    if (currentTime - lastSpeakTime > 15000) {
        if (getBooleanConfig('useLocalVideoForIdle') && sessionActive && !isSpeaking) {
            disconnectAvatar()
            const lv = document.getElementById('localVideo')
            if (lv) lv.hidden = false
            document.getElementById('remoteVideo').style.width = '0.1px'
            sessionActive = false
        }
    }
}

window.onload = () => {
    setInterval(() => {
        checkHung()
        checkLastSpeak()
    }, 2000) // Check session activity every 2 seconds
}

// Wire suggested question buttons to the chat handler (if present in DOM)
window.addEventListener('load', function() {
    try {
        const b1 = document.getElementById('suggestBtn1');
        const b2 = document.getElementById('suggestBtn2');
        if (b1) b1.addEventListener('click', () => handleUserQuery('hola quien eres'));
        if (b2) b2.addEventListener('click', () => handleUserQuery('ola sabes si va a haber una sesion de Copilot Studio en AgentCon Lima?'));
    } catch (e) { console.warn('Failed to wire suggested buttons', e); }
});

// Global keyboard shortcuts
;(function(){
    function isTypingTarget(el){
        if (!el) return false
        const tag = (el.tagName || '').toUpperCase()
        if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return true
        if (el.isContentEditable) return true
        return false
    }
    document.addEventListener('keydown', (e) => {
        // Use Alt+M to toggle microphone, Alt+S to stop speaking
        if (!e.altKey || e.ctrlKey || e.metaKey) return
        if (isTypingTarget(e.target)) return
        if (e.code === 'KeyM') {
            const micBtn = document.getElementById('microphone')
            if (micBtn && !micBtn.disabled) {
                e.preventDefault()
                try { window.microphone() } catch {}
            }
        } else if (e.code === 'KeyS') {
            const stopBtn = document.getElementById('stopSpeaking')
            if (stopBtn && !stopBtn.disabled) {
                e.preventDefault()
                try { stopSpeaking() } catch {}
            }
        }
    })
})()

window.startSession = async () => {
    lastInteractionTime = new Date()
    
    if (getBooleanConfig('useLocalVideoForIdle')) {
        // Safely update elements that might exist
        const startSessionBtn = document.getElementById('startSession');
        const configDiv = document.getElementById('configuration');
        const microphoneBtn = document.getElementById('microphone');
        const stopSessionBtn = document.getElementById('stopSession');
    const localVideoDiv = document.getElementById('localVideo');
        const remoteVideoDiv = document.getElementById('remoteVideo');
        const chatHistoryDiv = document.getElementById('chatHistory');
        const showTypeMessageElement = document.getElementById('showTypeMessage');
        
        if (startSessionBtn) startSessionBtn.disabled = true;
        if (configDiv) configDiv.hidden = true;
        if (microphoneBtn) microphoneBtn.disabled = false;
        if (stopSessionBtn) stopSessionBtn.disabled = false;
    if (localVideoDiv) localVideoDiv.hidden = false;
        if (remoteVideoDiv) remoteVideoDiv.style.width = '0.1px';
        if (chatHistoryDiv) chatHistoryDiv.hidden = false;
        if (showTypeMessageElement) showTypeMessageElement.disabled = false;
        return
    }

    userClosedSession = false
    await connectAvatar()
}

window.stopSession = () => {
    lastInteractionTime = new Date()
    document.getElementById('startSession').disabled = false
    document.getElementById('microphone').disabled = true
    document.getElementById('stopSession').disabled = true
    const configDiv = document.getElementById('configuration')
    if (configDiv) configDiv.hidden = false
    document.getElementById('chatHistory').hidden = true
    document.getElementById('showTypeMessage').checked = false
    document.getElementById('showTypeMessage').disabled = true
    const typeContainer = document.getElementById('typeMessageContainer')
    if (typeContainer) typeContainer.hidden = true
    // Reset mic overlay state
    isMicListening = false
    hideMicOverlay()
    if (getBooleanConfig('useLocalVideoForIdle')) {
        const lv = document.getElementById('localVideo')
        if (lv) lv.hidden = true
    }

    userClosedSession = true
    disconnectAvatar()
}

window.clearChatHistory = async () => {
    lastInteractionTime = new Date()
    document.getElementById('chatHistory').innerHTML = ''
    await initMessages()
}

window.microphone = () => {
    lastInteractionTime = new Date()
    if (document.getElementById('microphone').innerHTML === 'Stop Microphone') {
        // Stop microphone
        document.getElementById('microphone').disabled = true
        speechRecognizer.stopContinuousRecognitionAsync(
            () => {
                document.getElementById('microphone').innerHTML = 'Start Microphone'
                document.getElementById('microphone').disabled = false
                isMicListening = false
                updateMicOverlayUI()
            }, (err) => {
                console.log("Failed to stop continuous recognition:", err)
                document.getElementById('microphone').disabled = false
            })

        return
    }

    if (getBooleanConfig('useLocalVideoForIdle')) {
        if (!sessionActive) {
            connectAvatar()
        }

        setTimeout(() => {
            document.getElementById('audioPlayer').play()
        }, 5000)
    } else {
        document.getElementById('audioPlayer').play()
    }

    document.getElementById('microphone').disabled = true
    speechRecognizer.recognized = async (s, e) => {
        if (e.result.reason === SpeechSDK.ResultReason.RecognizedSpeech) {
            let userQuery = e.result.text.trim()
            if (userQuery === '') {
                return
            }

            // Auto stop microphone when a phrase is recognized, when it's not continuous conversation mode
            if (!getBooleanConfig('continuousConversation')) {
                document.getElementById('microphone').disabled = true
                speechRecognizer.stopContinuousRecognitionAsync(
                    () => {
                        document.getElementById('microphone').innerHTML = 'Start Microphone'
                        document.getElementById('microphone').disabled = false
                        isMicListening = false
                        updateMicOverlayUI()
                    }, (err) => {
                        console.log("Failed to stop continuous recognition:", err)
                        document.getElementById('microphone').disabled = false
                    })
            }

            handleUserQuery(userQuery,"","")
        }
    }

    speechRecognizer.startContinuousRecognitionAsync(
        () => {
            document.getElementById('microphone').innerHTML = 'Stop Microphone'
            document.getElementById('microphone').disabled = false
            isMicListening = true
            updateMicOverlayUI()
        }, (err) => {
            console.log("Failed to start continuous recognition:", err)
            document.getElementById('microphone').disabled = false
        })
}

window.updataEnableOyd = () => {
    if (document.getElementById('enableOyd').checked) {
        document.getElementById('cogSearchConfig').hidden = false
    } else {
        document.getElementById('cogSearchConfig').hidden = true
    }
}

window.updateTypeMessageBox = () => {
    if (document.getElementById('showTypeMessage').checked) {
        const container = document.getElementById('typeMessageContainer')
        const input = document.getElementById('userMessageInput')
        const sendBtn = document.getElementById('sendMessageBtn')
        if (container) container.hidden = false

        const sendMessage = () => {
            const text = (input.value || '').trim()
            if (!text) return
            handleUserQuery(text)
            input.value = ''
        }

        input.onkeydown = (e) => {
            if (e.key === 'Enter') {
                e.preventDefault()
                sendMessage()
            }
        }
        sendBtn.onclick = sendMessage
    } else {
        const container = document.getElementById('typeMessageContainer')
        if (container) container.hidden = true
    }
}

window.updateLocalVideoForIdle = () => {
    const idleEl = document.getElementById('useLocalVideoForIdle')
    const typeMsg = document.getElementById('showTypeMessageCheckbox')
    if (!typeMsg) return
    if (idleEl && idleEl.checked) {
        typeMsg.hidden = true
    } else {
        typeMsg.hidden = false
    }
}

window.updatePrivateEndpoint = () => {
    if (document.getElementById('enablePrivateEndpoint').checked) {
        document.getElementById('showPrivateEndpointCheckBox').hidden = false
    } else {
        document.getElementById('showPrivateEndpointCheckBox').hidden = true
    }
}

window.updateCustomAvatarBox = () => {
    const customized = document.getElementById('customizedAvatar')
    const useBuiltIn = document.getElementById('useBuiltInVoice')
    if (!useBuiltIn) return
    if (customized && customized.checked) {
        useBuiltIn.disabled = false
    } else {
        useBuiltIn.disabled = true
        useBuiltIn.checked = false
    }
}

// Configuration integration - load automatically when page loads
window.addEventListener('load', function() {
    console.log('Page loaded, checking configuration...');
    
    // Load configuration if available
    if (window.configManager) {
        const config = window.configManager.getConfiguration();
        console.log('Configuration loaded:', config);
        
        // Update status indicator
        const statusElement = document.getElementById('configStatus');
        if (statusElement) {
            if (config.APIKey && config.azureOpenAIApiKey) {
                statusElement.textContent = '✅ Configuration loaded';
                statusElement.style.color = '#4CAF50';
            } else {
                statusElement.textContent = '⚠️ Missing API keys - Configure first';
                statusElement.style.color = '#ff9800';
            }
        }
    } else {
        console.log('ConfigManager not available');
    }
    // Wire mic overlay button to toggle mic
    const micOverlayBtn = document.getElementById('micOverlayBtn')
    if (micOverlayBtn) {
        micOverlayBtn.addEventListener('click', function(){
            try { window.microphone() } catch {}
        })
    }
    // Periodically refresh overlay state
    setInterval(updateMicOverlayUI, 1500)
});