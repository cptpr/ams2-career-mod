using System.Text.Json;
using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Infrastructure.Diagnostics;

public sealed class RaceAutomationTraceWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly object _gate = new();
    private readonly string _traceFilePath;
    private readonly string _stateFilePath;
    private Guid? _currentRunId;
    private string _lastAutomationStatus = "No automation status yet.";
    private string _lastSessionStatus = "No session snapshot yet.";
    private string _lastExportStatus = "No export attempt yet.";
    private string _lastResultStatus = "No committed result yet.";
    private DateTime? _lastUpdatedUtc;

    public RaceAutomationTraceWriter(string diagnosticsDirectory)
    {
        Directory.CreateDirectory(diagnosticsDirectory);
        _traceFilePath = Path.Combine(diagnosticsDirectory, "race-automation.jsonl");
        _stateFilePath = Path.Combine(diagnosticsDirectory, "race-automation.state.json");
    }

    public string TraceFilePath => _traceFilePath;
    public string StateFilePath => _stateFilePath;

    public void Append(RaceAutomationStatus status)
    {
        try
        {
            lock (_gate)
            {
                _currentRunId = status.RunId;
                _lastAutomationStatus = $"{status.Stage}: {status.Headline}";
                _lastUpdatedUtc = status.TimestampUtc;
                AppendRecord(new RaceAutomationTraceRecord
                {
                    RecordType = "automation-status",
                    RunId = status.RunId,
                    TimestampUtc = status.TimestampUtc,
                    Stage = status.Stage.ToString(),
                    Headline = status.Headline,
                    Detail = status.Detail,
                    TelemetryLine = status.TelemetryLine,
                    RequiresGameRestart = status.RequiresGameRestart,
                    LaunchRequested = status.LaunchRequested
                });
                PersistStateUnsafe();
            }
        }
        catch
        {
        }
    }

    public void AppendSessionSnapshot(Guid? runId, SessionStatusSnapshot snapshot)
    {
        try
        {
            lock (_gate)
            {
                if (runId is not null)
                {
                    _currentRunId = runId;
                }

                _lastSessionStatus = snapshot.IsConnected
                    ? $"{snapshot.SessionPhase}: {snapshot.LeagueName} at {snapshot.TrackName}"
                    : "AMS2 is not connected.";
                _lastUpdatedUtc = DateTime.UtcNow;
                AppendRecord(new RaceAutomationTraceRecord
                {
                    RecordType = "session-snapshot",
                    RunId = runId,
                    TimestampUtc = DateTime.UtcNow,
                    IsConnected = snapshot.IsConnected,
                    SessionType = snapshot.SessionType.ToString(),
                    SessionPhase = snapshot.SessionPhase.ToString(),
                    LeagueId = snapshot.LeagueId,
                    LeagueName = snapshot.LeagueName,
                    TrackName = snapshot.TrackName,
                    ForceManualReview = snapshot.ForceManualReview
                });
                PersistStateUnsafe();
            }
        }
        catch
        {
        }
    }

    public void AppendDraftCreated(RaceResultDraft draft)
    {
        try
        {
            lock (_gate)
            {
                _currentRunId = draft.AutomationRunId;
                _lastResultStatus = draft.Summary;
                _lastUpdatedUtc = draft.CompletedUtc;
                AppendRecord(new RaceAutomationTraceRecord
                {
                    RecordType = "result-draft",
                    RunId = draft.AutomationRunId,
                    TimestampUtc = draft.CompletedUtc,
                    LeagueId = draft.LeagueId,
                    LeagueName = draft.LeagueName,
                    TrackName = draft.TrackName,
                    Outcome = draft.Outcome.ToString(),
                    OverallPosition = draft.OverallPosition,
                    ClassPosition = draft.ClassPosition,
                    Entrants = draft.Entrants,
                    LapsCompleted = draft.LapsCompleted,
                    Confidence = draft.Confidence.ToString(),
                    ValidationNotes = draft.ValidationNotes,
                    Summary = draft.Summary
                });
                PersistStateUnsafe();
            }
        }
        catch
        {
        }
    }

    public void AppendCommittedResult(Guid careerId, RaceResultConfirmed result)
    {
        try
        {
            lock (_gate)
            {
                _currentRunId = result.Draft.AutomationRunId;
                _lastResultStatus = $"{result.Draft.Outcome}: {result.Draft.Summary}";
                _lastUpdatedUtc = DateTime.UtcNow;
                AppendRecord(new RaceAutomationTraceRecord
                {
                    RecordType = "result-commit",
                    RunId = result.Draft.AutomationRunId,
                    TimestampUtc = DateTime.UtcNow,
                    CareerId = careerId,
                    ResultId = result.Id,
                    DraftId = result.Draft.Id,
                    WasReviewed = result.WasReviewed,
                    LeagueId = result.Draft.LeagueId,
                    LeagueName = result.Draft.LeagueName,
                    TrackName = result.Draft.TrackName,
                    Outcome = result.Draft.Outcome.ToString(),
                    OverallPosition = result.Draft.OverallPosition,
                    ClassPosition = result.Draft.ClassPosition,
                    Entrants = result.Draft.Entrants,
                    LapsCompleted = result.Draft.LapsCompleted,
                    Confidence = result.Draft.Confidence.ToString(),
                    Summary = result.Draft.Summary
                });
                PersistStateUnsafe();
            }
        }
        catch
        {
        }
    }

    public void AppendExportStatus(string status)
    {
        try
        {
            lock (_gate)
            {
                _lastExportStatus = status;
                _lastUpdatedUtc = DateTime.UtcNow;
                PersistStateUnsafe();
            }
        }
        catch
        {
        }
    }

    public bool TryLoadLatestState(out RaceAutomationDiagnosticsSnapshot snapshot)
    {
        try
        {
            if (!File.Exists(_stateFilePath))
            {
                snapshot = new RaceAutomationDiagnosticsSnapshot();
                return false;
            }

            var json = File.ReadAllText(_stateFilePath);
            var loaded = JsonSerializer.Deserialize<RaceAutomationDiagnosticsSnapshot>(json, JsonOptions);
            if (loaded is null)
            {
                snapshot = new RaceAutomationDiagnosticsSnapshot();
                return false;
            }

            snapshot = loaded;
            return true;
        }
        catch
        {
            snapshot = new RaceAutomationDiagnosticsSnapshot();
            return false;
        }
    }

    private void AppendRecord(RaceAutomationTraceRecord record)
    {
        var payload = JsonSerializer.Serialize(record, JsonOptions);
        File.AppendAllText(_traceFilePath, payload + Environment.NewLine);
    }

    private void PersistStateUnsafe()
    {
        var snapshot = new RaceAutomationDiagnosticsSnapshot
        {
            CurrentRunId = _currentRunId,
            LastAutomationStatus = _lastAutomationStatus,
            LastSessionStatus = _lastSessionStatus,
            LastExportStatus = _lastExportStatus,
            LastResultStatus = _lastResultStatus,
            LastUpdatedUtc = _lastUpdatedUtc ?? DateTime.UtcNow
        };

        File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    private sealed class RaceAutomationTraceRecord
    {
        public string RecordType { get; init; } = string.Empty;
        public Guid? RunId { get; init; }
        public DateTime TimestampUtc { get; init; }
        public string Stage { get; init; } = string.Empty;
        public string Headline { get; init; } = string.Empty;
        public string Detail { get; init; } = string.Empty;
        public string TelemetryLine { get; init; } = string.Empty;
        public bool RequiresGameRestart { get; init; }
        public bool LaunchRequested { get; init; }
        public bool? IsConnected { get; init; }
        public string SessionType { get; init; } = string.Empty;
        public string SessionPhase { get; init; } = string.Empty;
        public string LeagueId { get; init; } = string.Empty;
        public string LeagueName { get; init; } = string.Empty;
        public string TrackName { get; init; } = string.Empty;
        public bool? ForceManualReview { get; init; }
        public Guid? CareerId { get; init; }
        public Guid? ResultId { get; init; }
        public Guid? DraftId { get; init; }
        public bool? WasReviewed { get; init; }
        public string Outcome { get; init; } = string.Empty;
        public int? OverallPosition { get; init; }
        public int? ClassPosition { get; init; }
        public int? Entrants { get; init; }
        public int? LapsCompleted { get; init; }
        public string Confidence { get; init; } = string.Empty;
        public IReadOnlyList<string> ValidationNotes { get; init; } = Array.Empty<string>();
        public string Summary { get; init; } = string.Empty;
    }
}

public sealed class RaceAutomationDiagnosticsSnapshot
{
    public Guid? CurrentRunId { get; init; }
    public string LastAutomationStatus { get; init; } = "No automation status yet.";
    public string LastSessionStatus { get; init; } = "No session snapshot yet.";
    public string LastExportStatus { get; init; } = "No export attempt yet.";
    public string LastResultStatus { get; init; } = "No committed result yet.";
    public DateTime LastUpdatedUtc { get; init; }
}
