using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.App.ViewModels;

public sealed class RaceHistoryEntry
{
    public required Guid Id { get; init; }
    public required int OverallPosition { get; init; }
    public required int ClassPosition { get; init; }
    public required int Entrants { get; init; }
    public required int LapsCompleted { get; init; }
    public required bool IsCleanRace { get; init; }
    public required bool WasReviewed { get; init; }
    public Guid? AutomationRunId { get; init; }
    public required string Headline { get; init; }
    public required string Subtitle { get; init; }
    public required string ResultTag { get; init; }
    public required string CleanlinessTag { get; init; }
    public required string ReviewTag { get; init; }
    public required string DetailLine { get; init; }
    public required string Details { get; init; }

    public static RaceHistoryEntry From(RaceResultConfirmed result)
    {
        var draft = result.Draft;
        var completedLocal = draft.CompletedUtc == default
            ? "Unknown date"
            : draft.CompletedUtc.ToLocalTime().ToString("dd MMM yyyy  HH:mm");

        var resultTag = draft.OverallPosition switch
        {
            _ when draft.Outcome == RaceOutcome.Disqualified => "Disqualified",
            _ when draft.Outcome == RaceOutcome.Restarted => "Restarted",
            _ when draft.Outcome == RaceOutcome.Abandoned => "Abandoned",
            _ when draft.Outcome == RaceOutcome.Retired => "Retired",
            1 => "Win",
            2 or 3 => "Podium",
            <= 5 => "Top 5",
            <= 10 => "Top 10",
            _ => "Finish"
        };

        var cleanTag = draft.IsCleanRace ? "Clean race" : "Incident flagged";
        var reviewTag = result.WasReviewed
            ? "Manual review complete"
            : draft.Confidence == ResultConfidence.Low
                ? "Manual review needed"
                : "Auto logged";
        var confidenceTag = draft.Confidence switch
        {
            ResultConfidence.High => "High confidence capture",
            ResultConfidence.Medium => "Capture confirmed",
            _ => "Low confidence capture"
        };

        var reviewSummary = result.WasReviewed
            ? "Manual review complete"
            : "Result could not be confirmed automatically";

        return new RaceHistoryEntry
        {
            Id = result.Id,
            OverallPosition = draft.OverallPosition,
            ClassPosition = draft.ClassPosition,
            Entrants = draft.Entrants,
            LapsCompleted = draft.LapsCompleted,
            IsCleanRace = draft.IsCleanRace,
            WasReviewed = result.WasReviewed,
            AutomationRunId = draft.AutomationRunId,
            Headline = $"{NormalizeDisplayName(draft.LeagueName)} at {NormalizeDisplayName(draft.TrackName)}",
            Subtitle = completedLocal,
            ResultTag = resultTag,
            CleanlinessTag = cleanTag,
            ReviewTag = reviewTag,
            DetailLine = $"{resultTag}  |  P{draft.OverallPosition}/{draft.Entrants}  |  Class P{draft.ClassPosition}  |  {draft.LapsCompleted} laps",
            Details =
                $"{reviewSummary}\n" +
                $"{cleanTag}\n" +
                $"{confidenceTag}\n\n" +
                $"League: {NormalizeDisplayName(draft.LeagueName)}\n" +
                $"Track: {NormalizeDisplayName(draft.TrackName)}\n" +
                $"Completed: {completedLocal}\n" +
                $"Result: {draft.Outcome}\n" +
                $"Overall finish: P{draft.OverallPosition}/{draft.Entrants}\n" +
                $"Class finish: P{draft.ClassPosition}\n" +
                $"Laps completed: {draft.LapsCompleted}\n" +
                $"Validation notes: {(draft.ValidationNotes.Count == 0 ? "None" : string.Join("; ", draft.ValidationNotes))}\n" +
                $"Capture status: {cleanTag}\n" +
                $"Review status: {reviewTag}\n" +
                $"Automation run: {(draft.AutomationRunId?.ToString("D") ?? "Not linked")}\n\n" +
                $"Stored summary:\n{draft.Summary}"
        };
    }

    private static string NormalizeDisplayName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? value : value.Replace('_', ' ');
    }
}
