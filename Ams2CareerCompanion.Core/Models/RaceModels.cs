namespace Ams2CareerCompanion.Core.Models;

public enum SessionType
{
    None,
    Practice,
    Qualifying,
    Race
}

public enum SessionPhase
{
    Idle,
    Grid,
    Running,
    Finished
}

public enum ResultConfidence
{
    High,
    Medium,
    Low
}

public enum RaceOutcome
{
    Finished,
    Retired,
    Abandoned,
    Unknown
}

public enum TelemetryConnectionState
{
    Disconnected,
    Monitoring,
    Simulating
}

public sealed class SessionStatusSnapshot
{
    public bool IsConnected { get; init; }
    public SessionType SessionType { get; init; }
    public SessionPhase SessionPhase { get; init; }
    public string LeagueId { get; init; } = string.Empty;
    public string LeagueName { get; init; } = string.Empty;
    public string TrackName { get; init; } = string.Empty;
    public bool ForceManualReview { get; init; }
}

public sealed class TelemetrySnapshot
{
    public DateTime TimestampUtc { get; init; }
    public int CurrentLap { get; init; }
    public int CompletedLaps { get; init; }
    public int TotalLaps { get; init; }
    public int OverallPosition { get; init; }
    public int ClassPosition { get; init; }
    public int Entrants { get; init; }
    public double FuelLiters { get; init; }
    public bool IsInPit { get; init; }
    public bool WasCleanLap { get; init; }
    public uint ParticipantRaceState { get; init; }
}

public sealed class MockRaceScenario
{
    public string LeagueId { get; init; } = string.Empty;
    public string LeagueName { get; init; } = string.Empty;
    public string TrackName { get; init; } = string.Empty;
    public int GridSize { get; init; } = 12;
    public int PlannedFinishPosition { get; init; } = 6;
    public int PlannedClassPosition { get; init; } = 4;
    public int TotalLaps { get; init; } = 5;
    public bool ForceManualReview { get; init; }
    public bool IsCleanRace { get; init; } = true;
}

public sealed class RaceResultDraft
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string LeagueId { get; init; } = string.Empty;
    public string LeagueName { get; init; } = string.Empty;
    public string TrackName { get; init; } = string.Empty;
    public DateTime CompletedUtc { get; init; }
    public RaceOutcome Outcome { get; init; } = RaceOutcome.Unknown;
    public int OverallPosition { get; init; }
    public int ClassPosition { get; init; }
    public int Entrants { get; init; }
    public int LapsCompleted { get; init; }
    public bool IsCleanRace { get; init; }
    public ResultConfidence Confidence { get; init; }
    public string Summary { get; init; } = string.Empty;
}

public sealed class RaceResultConfirmed
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public RaceResultDraft Draft { get; init; } = new();
    public bool WasReviewed { get; init; }
}

public sealed class RewardBreakdown
{
    public int XpDelta { get; set; }
    public int CreditsDelta { get; set; }
    public double DriverRatingDelta { get; set; }
    public int ReputationDelta { get; set; }
    public bool LeagueCompleted { get; set; }
    public string? NewlyActiveLeagueName { get; set; }
    public List<string> Unlocks { get; set; } = new();
    public List<string> CompletedChallenges { get; set; } = new();
}

public sealed class CareerUpdateResult
{
    public RewardBreakdown RewardBreakdown { get; init; } = new();
    public RivalProfile? FeaturedRival { get; init; }
}
