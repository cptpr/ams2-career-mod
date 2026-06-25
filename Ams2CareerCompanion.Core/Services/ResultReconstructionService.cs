using Ams2CareerCompanion.Core.Interfaces;
using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Core.Services;

public sealed class ResultReconstructionService
{
    private SessionStatusSnapshot _lastStatus = new();
    private TelemetrySnapshot? _lastTelemetry;
    private bool _raceSessionActive;
    private bool _resultEmittedForCurrentSession;

    public event EventHandler<RaceResultDraft>? DraftCreated;

    public ResultReconstructionService(IGameTelemetryFeed telemetryFeed)
    {
        telemetryFeed.SessionStatusChanged += OnSessionStatusChanged;
        telemetryFeed.TelemetryReceived += OnTelemetryReceived;
    }

    private void OnTelemetryReceived(object? sender, TelemetrySnapshot snapshot)
    {
        _lastTelemetry = snapshot;
    }

    private void OnSessionStatusChanged(object? sender, SessionStatusSnapshot status)
    {
        if (status.SessionType == SessionType.Race && status.SessionPhase is SessionPhase.Grid or SessionPhase.Running)
        {
            _raceSessionActive = true;
        }

        if (_raceSessionActive && !_resultEmittedForCurrentSession && _lastStatus.SessionPhase == SessionPhase.Running && status.SessionPhase == SessionPhase.Finished)
        {
            _resultEmittedForCurrentSession = true;
            DraftCreated?.Invoke(this, BuildDraft(status, _lastTelemetry));
        }

        if (status.SessionType == SessionType.None && status.SessionPhase == SessionPhase.Idle)
        {
            _raceSessionActive = false;
            _resultEmittedForCurrentSession = false;
            _lastTelemetry = null;
        }

        _lastStatus = status;
    }

    private static RaceResultDraft BuildDraft(SessionStatusSnapshot status, TelemetrySnapshot? telemetry)
    {
        if (telemetry is null)
        {
            return new RaceResultDraft
            {
                LeagueId = status.LeagueId,
                LeagueName = status.LeagueName,
                TrackName = status.TrackName,
                CompletedUtc = DateTime.UtcNow,
                Confidence = ResultConfidence.Low,
                Summary = "Telemetry was missing at session end. Manual confirmation required."
            };
        }

        var confidence = status.ForceManualReview
            ? ResultConfidence.Medium
            : telemetry.CompletedLaps >= 3
                ? ResultConfidence.High
                : ResultConfidence.Low;

        var summary = $"{status.LeagueName} finished at {status.TrackName}: P{telemetry.OverallPosition}/{telemetry.Entrants}, " +
                      $"class P{telemetry.ClassPosition}, {telemetry.CompletedLaps} laps.";

        return new RaceResultDraft
        {
            LeagueId = status.LeagueId,
            LeagueName = status.LeagueName,
            TrackName = status.TrackName,
            CompletedUtc = DateTime.UtcNow,
            OverallPosition = telemetry.OverallPosition,
            ClassPosition = telemetry.ClassPosition,
            Entrants = telemetry.Entrants,
            LapsCompleted = telemetry.CompletedLaps,
            IsCleanRace = telemetry.WasCleanLap,
            Confidence = confidence,
            Summary = summary
        };
    }
}
