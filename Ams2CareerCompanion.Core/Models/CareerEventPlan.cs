namespace Ams2CareerCompanion.Core.Models;

public sealed class CareerEventPlan
{
    public string EventTemplateId { get; init; } = string.Empty;
    public string EventTemplateName { get; init; } = string.Empty;
    public string LeagueId { get; init; } = string.Empty;
    public string LeagueName { get; init; } = string.Empty;
    public int EventNumber { get; init; }
    public int EventCount { get; init; }
    public string TrackLayoutId { get; init; } = string.Empty;
    public string TrackDisplayName { get; init; } = string.Empty;
    public string TrackId { get; init; } = string.Empty;
    public string TrackName { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string PlayerCarClassId { get; init; } = string.Empty;
    public string PlayerCarClassName { get; init; } = string.Empty;
    public int RecommendedGridSize { get; init; }
    public int RecommendedDriverRating { get; init; }
    public int BaseXpReward { get; init; }
    public int BaseCreditReward { get; init; }
    public string ExportAdapterId { get; init; } = string.Empty;
    public bool RequiresGameRestart { get; init; }
    public string SuggestedPresetSlug { get; init; } = string.Empty;
    public string SetupNotes { get; init; } = string.Empty;
}
