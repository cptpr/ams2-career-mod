using Ams2CareerCompanion.Core.Models;
using Ams2CareerCompanion.Infrastructure.Launch;

namespace Ams2CareerCompanion.App.ViewModels;

public sealed class EventPlanViewModel : ObservableObject
{
    private bool _hasPlan;
    private string _headline = "No next event available.";
    private string _details = "Create or load a career to generate the next event plan.";
    private string _exportStatus = "No export target selected yet.";
    private CareerEventPlan? _plan;
    private EventExportReadiness? _readiness;

    public bool HasPlan
    {
        get => _hasPlan;
        private set => SetProperty(ref _hasPlan, value);
    }

    public string Headline
    {
        get => _headline;
        private set => SetProperty(ref _headline, value);
    }

    public string Details
    {
        get => _details;
        private set => SetProperty(ref _details, value);
    }

    public string ExportStatus
    {
        get => _exportStatus;
        private set => SetProperty(ref _exportStatus, value);
    }

    public CareerEventPlan? Plan
    {
        get => _plan;
        private set => SetProperty(ref _plan, value);
    }

    public EventExportReadiness? Readiness
    {
        get => _readiness;
        private set => SetProperty(ref _readiness, value);
    }

    public void Clear()
    {
        HasPlan = false;
        Plan = null;
        Readiness = null;
        Headline = "No next event available.";
        Details = "Create or load a career to generate the next event plan.";
        ExportStatus = "No export target selected yet.";
    }

    public void Update(CareerEventPlan plan, string details, string exportStatus, EventExportReadiness? readiness)
    {
        HasPlan = true;
        Plan = plan;
        Readiness = readiness;
        Headline = $"Next Event: {plan.LeagueName} at {plan.TrackDisplayName}  |  Round {plan.EventNumber}/{plan.EventCount}";
        Details = details;
        ExportStatus = exportStatus;
    }
}
