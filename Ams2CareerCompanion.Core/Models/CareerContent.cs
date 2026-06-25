namespace Ams2CareerCompanion.Core.Models;

public enum CareerPreset
{
    Casual,
    Standard,
    Hardcore
}

public enum ChallengeCadence
{
    Daily,
    Weekly
}

public enum ChallengeKind
{
    FinishRace,
    CleanFinish,
    PodiumFinish
}

public sealed class StarterCarDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DisplayLabel => $"{Name} ({ClassName})";
}

public sealed class LeagueDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string TrackName { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public IReadOnlyList<string> PrerequisiteLeagueIds { get; init; } = Array.Empty<string>();
    public int RequiredLevel { get; init; }
    public int RequiredReputation { get; init; }
    public int GridSize { get; init; } = 12;
    public int RequiredCompletionPosition { get; init; } = 3;
    public int RecommendedDriverRating { get; init; }
    public int BaseXpReward { get; init; }
    public int BaseCreditReward { get; init; }
}

public sealed class CareerTitleDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int RequiredLevel { get; init; }
}

public sealed class ChallengeTemplate
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ChallengeCadence Cadence { get; init; }
    public ChallengeKind Kind { get; init; }
    public int Target { get; init; }
    public int BonusXp { get; init; }
    public int BonusCredits { get; init; }
}

public sealed class RivalArchetype
{
    public string Name { get; init; } = string.Empty;
    public string Personality { get; init; } = string.Empty;
    public string Specialty { get; init; } = string.Empty;
    public double BaseRating { get; init; }
}

public sealed class CareerContentCatalog
{
    public IReadOnlyList<StarterCarDefinition> StarterCars { get; init; } = Array.Empty<StarterCarDefinition>();
    public IReadOnlyList<LeagueDefinition> Leagues { get; init; } = Array.Empty<LeagueDefinition>();
    public IReadOnlyList<CareerTitleDefinition> Titles { get; init; } = Array.Empty<CareerTitleDefinition>();
    public IReadOnlyList<ChallengeTemplate> ChallengeTemplates { get; init; } = Array.Empty<ChallengeTemplate>();
    public IReadOnlyList<RivalArchetype> RivalArchetypes { get; init; } = Array.Empty<RivalArchetype>();
}
