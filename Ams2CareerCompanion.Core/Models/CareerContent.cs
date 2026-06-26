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
    Weekly,
    Monthly
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
    public string CarId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DisplayLabel => $"{Name} ({ClassName})";
}

public sealed class OfficialCarClassDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Family { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public bool IsStarterEligible { get; init; }
    public bool IsDlc { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class OfficialCarDefinition
{
    public string Id { get; init; } = string.Empty;
    public string ClassId { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string TeamName { get; init; } = string.Empty;
    public bool HasLights { get; init; }
    public bool IsDlc { get; init; }
}

public sealed class OfficialTrackDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public bool IsDlc { get; init; }
}

public sealed class OfficialTrackLayoutDefinition
{
    public string Id { get; init; } = string.Empty;
    public string TrackId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int RecommendedGridSize { get; init; }
    public string Grade { get; init; } = string.Empty;
    public bool SupportsKarts { get; init; }
}

public sealed class LeagueDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string TrackName { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public IReadOnlyList<string> TrackLayoutIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> EligibleCarClassIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PrerequisiteLeagueIds { get; init; } = Array.Empty<string>();
    public int RequiredLevel { get; init; }
    public int RequiredReputation { get; init; }
    public int GridSize { get; init; } = 12;
    public int RequiredCompletionPosition { get; init; } = 3;
    public int RecommendedDriverRating { get; init; }
    public int BaseXpReward { get; init; }
    public int BaseCreditReward { get; init; }
}

public sealed class EventTemplateDefinition
{
    public string Id { get; init; } = string.Empty;
    public string LeagueId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string TrackLayoutId { get; init; } = string.Empty;
    public IReadOnlyList<string> EligibleCarClassIds { get; init; } = Array.Empty<string>();
    public string ExportAdapterId { get; init; } = "championship-editor-preset";
    public string PresetSlug { get; init; } = string.Empty;
    public bool RequiresGameRestart { get; init; } = true;
    public int? RecommendedGridSizeOverride { get; init; }
    public string SetupInstructions { get; init; } = string.Empty;
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
    public IReadOnlyList<OfficialCarClassDefinition> CarClasses { get; init; } = Array.Empty<OfficialCarClassDefinition>();
    public IReadOnlyList<OfficialCarDefinition> Cars { get; init; } = Array.Empty<OfficialCarDefinition>();
    public IReadOnlyList<OfficialTrackDefinition> Tracks { get; init; } = Array.Empty<OfficialTrackDefinition>();
    public IReadOnlyList<OfficialTrackLayoutDefinition> TrackLayouts { get; init; } = Array.Empty<OfficialTrackLayoutDefinition>();
    public IReadOnlyList<StarterCarDefinition> StarterCars { get; init; } = Array.Empty<StarterCarDefinition>();
    public IReadOnlyList<LeagueDefinition> Leagues { get; init; } = Array.Empty<LeagueDefinition>();
    public IReadOnlyList<EventTemplateDefinition> EventTemplates { get; init; } = Array.Empty<EventTemplateDefinition>();
    public IReadOnlyList<CareerTitleDefinition> Titles { get; init; } = Array.Empty<CareerTitleDefinition>();
    public IReadOnlyList<ChallengeTemplate> ChallengeTemplates { get; init; } = Array.Empty<ChallengeTemplate>();
    public IReadOnlyList<RivalArchetype> RivalArchetypes { get; init; } = Array.Empty<RivalArchetype>();
}
