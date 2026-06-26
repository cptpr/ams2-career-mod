using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Infrastructure.Launch;

public sealed class ChampionshipEditorPresetExportAdapter
{
    private readonly Ams2SessionPresetService _sessionPresetService;

    public ChampionshipEditorPresetExportAdapter(Ams2SessionPresetService sessionPresetService)
    {
        _sessionPresetService = sessionPresetService;
    }

    public EventExportPreview BuildPreview(CareerEventPlan? eventPlan)
    {
        if (eventPlan is null)
        {
            return new EventExportPreview(false, "No event plan is available yet.", Readiness: BuildUnavailableReadiness("No event plan is available yet."));
        }

        if (!string.Equals(eventPlan.ExportAdapterId, "championship-editor-preset", StringComparison.Ordinal))
        {
            return new EventExportPreview(false, $"Unsupported export adapter '{eventPlan.ExportAdapterId}'.", Readiness: BuildUnavailableReadiness($"Unsupported export adapter '{eventPlan.ExportAdapterId}'."));
        }

        var environment = _sessionPresetService.GetEnvironmentReport();
        if (!environment.IsReady)
        {
            return new EventExportPreview(false, environment.Message, Readiness: ToReadiness(environment, null));
        }

        var preset = _sessionPresetService.FindPresetBySlug(eventPlan.SuggestedPresetSlug);
        if (preset is null)
        {
            return new EventExportPreview(
                false,
                $"No preset found for slug '{eventPlan.SuggestedPresetSlug}'.",
                Readiness: ToReadiness(environment, null, $"Preset slug '{eventPlan.SuggestedPresetSlug}' is not available in the preset library."));
        }

        return new EventExportPreview(
            true,
            $"Preset '{preset.Name}' is ready for {eventPlan.EventTemplateName}.",
            preset,
            eventPlan.RequiresGameRestart,
            ToReadiness(environment, preset));
    }

    public EventExportResult Apply(CareerEventPlan? eventPlan)
    {
        var preview = BuildPreview(eventPlan);
        if (!preview.Success)
        {
            return new EventExportResult(false, preview.Message, Readiness: preview.Readiness);
        }

        var applyResult = _sessionPresetService.ApplyPreset(preview.Preset);
        if (!applyResult.Success)
        {
            return new EventExportResult(false, applyResult.Message, preview.Preset, preview.RequiresGameRestart, preview.Readiness);
        }

        var restartText = preview.RequiresGameRestart
            ? " Restart AMS2 before entering the event to ensure the prepared race state is loaded."
            : string.Empty;

        return new EventExportResult(
            true,
            $"{applyResult.Message}{restartText}",
            preview.Preset,
            preview.RequiresGameRestart,
            preview.Readiness);
    }

    private static EventExportReadiness BuildUnavailableReadiness(string blockingIssue)
    {
        return new EventExportReadiness(
            false,
            null,
            null,
            null,
            [blockingIssue],
            Array.Empty<string>());
    }

    private static EventExportReadiness ToReadiness(SessionPresetEnvironmentReport environment, SessionPresetInfo? preset, string? extraBlockingIssue = null)
    {
        var blockingIssues = new List<string>();
        if (!environment.IsReady)
        {
            blockingIssues.Add(environment.Message);
        }

        if (!string.IsNullOrWhiteSpace(extraBlockingIssue))
        {
            blockingIssues.Add(extraBlockingIssue);
        }

        return new EventExportReadiness(
            environment.IsReady && string.IsNullOrWhiteSpace(extraBlockingIssue),
            preset?.Name,
            preset?.FilePath,
            environment.TargetFilePath,
            blockingIssues,
            environment.Guidance);
    }
}

public sealed record EventExportPreview(
    bool Success,
    string Message,
    SessionPresetInfo? Preset = null,
    bool RequiresGameRestart = false,
    EventExportReadiness? Readiness = null);

public sealed record EventExportResult(
    bool Success,
    string Message,
    SessionPresetInfo? Preset = null,
    bool RequiresGameRestart = false,
    EventExportReadiness? Readiness = null);

public sealed record EventExportReadiness(
    bool IsReady,
    string? PresetName,
    string? PresetFilePath,
    string? TargetFilePath,
    IReadOnlyList<string> BlockingIssues,
    IReadOnlyList<string> Guidance);
