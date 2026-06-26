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
            return new EventExportPreview(false, "No event plan is available yet.");
        }

        if (!string.Equals(eventPlan.ExportAdapterId, "championship-editor-preset", StringComparison.Ordinal))
        {
            return new EventExportPreview(false, $"Unsupported export adapter '{eventPlan.ExportAdapterId}'.");
        }

        var preset = _sessionPresetService.FindPresetBySlug(eventPlan.SuggestedPresetSlug);
        if (preset is null)
        {
            return new EventExportPreview(false, $"No preset found for slug '{eventPlan.SuggestedPresetSlug}'.");
        }

        return new EventExportPreview(
            true,
            $"Preset '{preset.Name}' is ready for {eventPlan.EventTemplateName}.",
            preset,
            eventPlan.RequiresGameRestart);
    }

    public EventExportResult Apply(CareerEventPlan? eventPlan)
    {
        var preview = BuildPreview(eventPlan);
        if (!preview.Success)
        {
            return new EventExportResult(false, preview.Message);
        }

        var applyResult = _sessionPresetService.ApplyPreset(preview.Preset);
        if (!applyResult.Success)
        {
            return new EventExportResult(false, applyResult.Message);
        }

        var restartText = preview.RequiresGameRestart
            ? " Restart AMS2 before entering the event to ensure the prepared race state is loaded."
            : string.Empty;

        return new EventExportResult(
            true,
            $"{applyResult.Message}{restartText}",
            preview.Preset,
            preview.RequiresGameRestart);
    }
}

public sealed record EventExportPreview(
    bool Success,
    string Message,
    SessionPresetInfo? Preset = null,
    bool RequiresGameRestart = false);

public sealed record EventExportResult(
    bool Success,
    string Message,
    SessionPresetInfo? Preset = null,
    bool RequiresGameRestart = false);
