namespace Ams2CareerCompanion.App.ViewModels;

public sealed class DiagnosticsSummaryViewModel : ObservableObject
{
    private Guid? _currentRunId;
    private string _automationTracePath = string.Empty;
    private string _telemetryLogPath = string.Empty;
    private string _startupErrorLogPath = string.Empty;
    private string _lastAutomationStatus = "No automation status yet.";
    private string _lastSessionStatus = "No session snapshot yet.";
    private string _lastExportStatus = "No export attempt yet.";
    private string _lastResultStatus = "No committed result yet.";
    private DateTime? _lastUpdatedUtc;

    public Guid? CurrentRunId
    {
        get => _currentRunId;
        private set => SetProperty(ref _currentRunId, value);
    }

    public string AutomationTracePath
    {
        get => _automationTracePath;
        private set => SetProperty(ref _automationTracePath, value);
    }

    public string TelemetryLogPath
    {
        get => _telemetryLogPath;
        private set => SetProperty(ref _telemetryLogPath, value);
    }

    public string StartupErrorLogPath
    {
        get => _startupErrorLogPath;
        private set => SetProperty(ref _startupErrorLogPath, value);
    }

    public string LastAutomationStatus
    {
        get => _lastAutomationStatus;
        private set => SetProperty(ref _lastAutomationStatus, value);
    }

    public string LastSessionStatus
    {
        get => _lastSessionStatus;
        private set => SetProperty(ref _lastSessionStatus, value);
    }

    public string LastExportStatus
    {
        get => _lastExportStatus;
        private set => SetProperty(ref _lastExportStatus, value);
    }

    public string LastResultStatus
    {
        get => _lastResultStatus;
        private set => SetProperty(ref _lastResultStatus, value);
    }

    public DateTime? LastUpdatedUtc
    {
        get => _lastUpdatedUtc;
        private set => SetProperty(ref _lastUpdatedUtc, value);
    }

    public void InitializePaths(string automationTracePath, string telemetryLogPath, string startupErrorLogPath)
    {
        AutomationTracePath = automationTracePath;
        TelemetryLogPath = telemetryLogPath;
        StartupErrorLogPath = startupErrorLogPath;
        Touch();
    }

    public void RecordAutomationStatus(Guid? runId, string status)
    {
        CurrentRunId = runId;
        LastAutomationStatus = status;
        Touch();
    }

    public void RecordSessionStatus(Guid? runId, string status)
    {
        if (runId is not null)
        {
            CurrentRunId = runId;
        }

        LastSessionStatus = status;
        Touch();
    }

    public void RecordExportStatus(string status)
    {
        LastExportStatus = status;
        Touch();
    }

    public void RecordResultStatus(Guid? runId, string status)
    {
        if (runId is not null)
        {
            CurrentRunId = runId;
        }

        LastResultStatus = status;
        Touch();
    }

    private void Touch()
    {
        LastUpdatedUtc = DateTime.UtcNow;
    }
}
