using System.Text.Json;
using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Infrastructure.Diagnostics;

public sealed class RaceAutomationTraceWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly string _traceFilePath;

    public RaceAutomationTraceWriter(string diagnosticsDirectory)
    {
        Directory.CreateDirectory(diagnosticsDirectory);
        _traceFilePath = Path.Combine(diagnosticsDirectory, "race-automation.jsonl");
    }

    public string TraceFilePath => _traceFilePath;

    public void Append(RaceAutomationStatus status)
    {
        try
        {
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
        }
        catch
        {
        }
    }

    public void AppendSessionSnapshot(Guid? runId, SessionStatusSnapshot snapshot)
    {
        try
        {
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
        }
        catch
        {
        }
    }

    public void AppendCommittedResult(Guid careerId, RaceResultConfirmed result)
    {
        try
        {
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
        }
        catch
        {
        }
    }

    private void AppendRecord(RaceAutomationTraceRecord record)
    {
        var payload = JsonSerializer.Serialize(record, JsonOptions);
        File.AppendAllText(_traceFilePath, payload + Environment.NewLine);
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
        public string Summary { get; init; } = string.Empty;
    }
}
