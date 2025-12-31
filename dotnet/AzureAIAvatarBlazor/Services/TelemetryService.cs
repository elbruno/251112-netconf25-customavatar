using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AzureAIAvatarBlazor.Services;

/// <summary>
/// Service for tracking custom telemetry events specific to avatar operations
/// </summary>
public class TelemetryService : IDisposable
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Counter<int> _avatarSessionCounter;
    private readonly Counter<int> _chatMessageCounter;
    private readonly Histogram<double> _aiResponseTimeHistogram;
    private readonly Histogram<double> _avatarSessionDurationHistogram;
    private bool _disposed;

    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger;
        
        // Create activity source for distributed tracing
        _activitySource = new ActivitySource("AzureAIAvatarBlazor");
        
        // Create meter for metrics
        _meter = new Meter("AzureAIAvatarBlazor");
        
        // Define custom metrics
        _avatarSessionCounter = _meter.CreateCounter<int>(
            "avatar.sessions.started",
            description: "Number of avatar sessions started");
        
        _chatMessageCounter = _meter.CreateCounter<int>(
            "chat.messages.sent",
            description: "Number of chat messages sent");
        
        _aiResponseTimeHistogram = _meter.CreateHistogram<double>(
            "ai.response.duration",
            unit: "ms",
            description: "AI response time in milliseconds");
        
        _avatarSessionDurationHistogram = _meter.CreateHistogram<double>(
            "avatar.session.duration",
            unit: "s",
            description: "Avatar session duration in seconds");
    }

    /// <summary>
    /// Track avatar session start
    /// </summary>
    public void TrackAvatarSessionStart(string character, string style, bool isCustomAvatar)
    {
        _avatarSessionCounter.Add(1,
            new KeyValuePair<string, object?>("character", character),
            new KeyValuePair<string, object?>("style", style),
            new KeyValuePair<string, object?>("is_custom", isCustomAvatar));

        _logger.LogInformation(
            "Avatar session started: Character={Character}, Style={Style}, IsCustom={IsCustom}",
            character, style, isCustomAvatar);
    }

    /// <summary>
    /// Track avatar session end
    /// </summary>
    public void TrackAvatarSessionEnd(string character, double durationSeconds)
    {
        _avatarSessionDurationHistogram.Record(durationSeconds,
            new KeyValuePair<string, object?>("character", character));

        _logger.LogInformation(
            "Avatar session ended: Character={Character}, Duration={Duration}s",
            character, durationSeconds);
    }

    /// <summary>
    /// Track chat message sent
    /// </summary>
    public void TrackChatMessage(string role, int messageLength)
    {
        _chatMessageCounter.Add(1,
            new KeyValuePair<string, object?>("role", role));

        _logger.LogInformation(
            "Chat message: Role={Role}, Length={Length}",
            role, messageLength);
    }

    /// <summary>
    /// Track AI response time
    /// </summary>
    public void TrackAIResponseTime(string mode, double durationMs, int? characterCount = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("mode", mode)
        };

        if (characterCount.HasValue)
        {
            tags.Add(new("characters", characterCount.Value));
        }

        _aiResponseTimeHistogram.Record(durationMs, tags.ToArray());

        _logger.LogInformation(
            "AI response: Mode={Mode}, Duration={Duration}ms, Characters={Characters}",
            mode, durationMs, characterCount);
    }

    /// <summary>
    /// Create a custom activity (span) for distributed tracing
    /// </summary>
    public Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
    {
        return _activitySource.StartActivity(operationName, kind);
    }

    /// <summary>
    /// Start avatar session span with rich attributes
    /// </summary>
    public Activity? StartAvatarSessionSpan(string character, string style, bool isCustomAvatar)
    {
        var activity = _activitySource.StartActivity("AvatarSession.Start", ActivityKind.Internal);
        activity?.SetTag("avatar.character", character);
        activity?.SetTag("avatar.style", style);
        activity?.SetTag("avatar.is_custom", isCustomAvatar);
        return activity;
    }

    /// <summary>
    /// Start AI agent chat completion span with rich attributes
    /// </summary>
    public Activity? StartAIAgentChatSpan(string mode, string modelOrAgent, int messageLength)
    {
        var activity = _activitySource.StartActivity("AIAgent.ChatCompletion", ActivityKind.Client);
        activity?.SetTag("ai.agent.mode", mode);
        activity?.SetTag("ai.model.name", modelOrAgent);
        activity?.SetTag("ai.prompt.length", messageLength);
        return activity;
    }

    /// <summary>
    /// Start AI agent initialization span
    /// </summary>
    public Activity? StartAIAgentInitSpan(string mode, string? endpoint = null)
    {
        var activity = _activitySource.StartActivity("AIAgent.Initialize", ActivityKind.Internal);
        activity?.SetTag("ai.agent.mode", mode);
        if (!string.IsNullOrEmpty(endpoint))
        {
            activity?.SetTag("ai.endpoint", endpoint);
        }
        return activity;
    }

    /// <summary>
    /// Start speech synthesis span
    /// </summary>
    public Activity? StartSpeechSynthesisSpan(string voice, int textLength)
    {
        var activity = _activitySource.StartActivity("Speech.Synthesize", ActivityKind.Client);
        activity?.SetTag("speech.voice", voice);
        activity?.SetTag("speech.text_length", textLength);
        return activity;
    }

    /// <summary>
    /// Start configuration load span
    /// </summary>
    public Activity? StartConfigLoadSpan(string source)
    {
        var activity = _activitySource.StartActivity("Config.Load", ActivityKind.Internal);
        activity?.SetTag("config.source", source);
        return activity;
    }

    /// <summary>
    /// Start configuration save span
    /// </summary>
    public Activity? StartConfigSaveSpan(int changedKeyCount)
    {
        var activity = _activitySource.StartActivity("Config.Save", ActivityKind.Internal);
        activity?.SetTag("config.changed_keys", changedKeyCount);
        return activity;
    }

    /// <summary>
    /// Track configuration change
    /// </summary>
    public void TrackConfigurationChange(string settingName, string? oldValue, string? newValue)
    {
        _logger.LogInformation(
            "Configuration changed: Setting={Setting}, OldValue={OldValue}, NewValue={NewValue}",
            settingName, oldValue, newValue);
    }

    /// <summary>
    /// Track error
    /// </summary>
    public void TrackError(string operation, Exception ex)
    {
        _logger.LogError(ex,
            "Error in operation: {Operation}",
            operation);
    }

    /// <summary>
    /// Track speech synthesis operation
    /// </summary>
    public void TrackSpeechSynthesis(string voice, int textLength, double durationMs)
    {
        _logger.LogInformation(
            "Speech synthesis: Voice={Voice}, TextLength={Length}, Duration={Duration}ms",
            voice, textLength, durationMs);
    }

    /// <summary>
    /// Track WebRTC connection status
    /// </summary>
    public void TrackWebRTCConnection(string status, string? errorMessage = null)
    {
        if (string.IsNullOrEmpty(errorMessage))
        {
            _logger.LogInformation("WebRTC connection: Status={Status}", status);
        }
        else
        {
            _logger.LogWarning("WebRTC connection: Status={Status}, Error={Error}", status, errorMessage);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _activitySource?.Dispose();
        _meter?.Dispose();
        _disposed = true;
    }
}
