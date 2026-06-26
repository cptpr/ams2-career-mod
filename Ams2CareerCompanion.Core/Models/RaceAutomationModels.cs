namespace Ams2CareerCompanion.Core.Models;

public enum RaceAutomationStage
{
    Idle,
    EventPrepared,
    RestartRequired,
    LaunchRequested,
    WaitingForSession,
    GridDetected,
    RaceRunning,
    SessionFinished,
    Error
}

public sealed class RaceAutomationStatus
{
    public Guid? RunId { get; init; }
    public RaceAutomationStage Stage { get; init; } = RaceAutomationStage.Idle;
    public string Headline { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string TelemetryLine { get; init; } = string.Empty;
    public bool RequiresGameRestart { get; init; }
    public bool LaunchRequested { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
