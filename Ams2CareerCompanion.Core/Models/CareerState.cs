namespace Ams2CareerCompanion.Core.Models;

public sealed class PlayerProfile
{
    public string StarterCarId { get; set; } = string.Empty;
    public string StarterCarName { get; set; } = string.Empty;
    public string StarterCarClass { get; set; } = string.Empty;
    public string SelectedCarClass { get; set; } = string.Empty;
}

public sealed class ProgressionState
{
    public int Xp { get; set; }
    public int Credits { get; set; }
    public double DriverRating { get; set; } = 1000;
    public int Reputation { get; set; }
    public int Level { get; set; } = 1;
}

public sealed class RivalProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string CurrentLeagueId { get; set; } = string.Empty;
    public double DriverRating { get; set; }
    public int Reputation { get; set; }
    public int RivalryIntensity { get; set; }
}

public sealed class ChallengeInstance
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ChallengeCadence Cadence { get; set; }
    public ChallengeKind Kind { get; set; }
    public int Target { get; set; }
    public int Progress { get; set; }
    public int BonusXp { get; set; }
    public int BonusCredits { get; set; }
    public bool IsCompleted { get; set; }
}

public sealed class CareerState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public CareerPreset Preset { get; set; } = CareerPreset.Standard;
    public PlayerProfile PlayerProfile { get; set; } = new();
    public ProgressionState Progression { get; set; } = new();
    public string ActiveLeagueId { get; set; } = string.Empty;
    public List<string> UnlockedLeagueIds { get; set; } = new();
    public List<string> CompletedLeagueIds { get; set; } = new();
    public List<string> UnlockedTitleIds { get; set; } = new();
    public List<RivalProfile> Rivals { get; set; } = new();
    public List<ChallengeInstance> Challenges { get; set; } = new();
}

public sealed class CareerSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
    public int Level { get; init; }
    public string ActiveLeagueId { get; init; } = string.Empty;
    public string DisplayLabel => $"{Name}  |  Level {Level}  |  {CreatedUtc:yyyy-MM-dd}";
}
