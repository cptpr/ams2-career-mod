using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.App.ViewModels;

public sealed class AutomationStateViewModel : ObservableObject
{
    private Guid? _runId;
    private RaceAutomationStage _stage;
    private string _headline = "No race automation active.";
    private string _detail = "Generate or load a career event to prepare the next race.";
    private string _telemetryLine = string.Empty;
    private bool _requiresGameRestart;
    private bool _launchRequested;
    private DateTime _timestampUtc = DateTime.UtcNow;

    public Guid? RunId
    {
        get => _runId;
        private set => SetProperty(ref _runId, value);
    }

    public RaceAutomationStage Stage
    {
        get => _stage;
        private set => SetProperty(ref _stage, value);
    }

    public string Headline
    {
        get => _headline;
        private set => SetProperty(ref _headline, value);
    }

    public string Detail
    {
        get => _detail;
        private set => SetProperty(ref _detail, value);
    }

    public string TelemetryLine
    {
        get => _telemetryLine;
        private set => SetProperty(ref _telemetryLine, value);
    }

    public bool RequiresGameRestart
    {
        get => _requiresGameRestart;
        private set => SetProperty(ref _requiresGameRestart, value);
    }

    public bool LaunchRequested
    {
        get => _launchRequested;
        private set => SetProperty(ref _launchRequested, value);
    }

    public DateTime TimestampUtc
    {
        get => _timestampUtc;
        private set => SetProperty(ref _timestampUtc, value);
    }

    public void Update(RaceAutomationStatus status)
    {
        RunId = status.RunId;
        Stage = status.Stage;
        Headline = status.Headline;
        Detail = status.Detail;
        TelemetryLine = status.TelemetryLine;
        RequiresGameRestart = status.RequiresGameRestart;
        LaunchRequested = status.LaunchRequested;
        TimestampUtc = status.TimestampUtc;
    }
}
