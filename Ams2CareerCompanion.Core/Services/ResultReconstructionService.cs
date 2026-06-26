using Ams2CareerCompanion.Core.Interfaces;
using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Core.Services;

public sealed class ResultReconstructionService
{
    private SessionStatusSnapshot _lastStatus = new();
    private TelemetrySnapshot? _lastTelemetry;
    private bool _raceSessionActive;
    private bool _resultEmittedForCurrentSession;
    private bool _sawRunningPhase;

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
            _sawRunningPhase |= status.SessionPhase == SessionPhase.Running;
        }

        if (_raceSessionActive && !_resultEmittedForCurrentSession && _lastStatus.SessionPhase == SessionPhase.Running && status.SessionPhase == SessionPhase.Finished)
        {
            _resultEmittedForCurrentSession = true;
            DraftCreated?.Invoke(this, BuildDraft(status, _lastTelemetry, RaceOutcome.Finished));
        }

        if (status.SessionType == SessionType.None && status.SessionPhase == SessionPhase.Idle)
        {
            if (_raceSessionActive && !_resultEmittedForCurrentSession && _sawRunningPhase)
            {
                _resultEmittedForCurrentSession = true;
                DraftCreated?.Invoke(this, BuildDraft(_lastStatus, _lastTelemetry, RaceOutcome.Abandoned));
            }

            _raceSessionActive = false;
            _resultEmittedForCurrentSession = false;
            _sawRunningPhase = false;
            _lastTelemetry = null;
        }

        _lastStatus = status;
    }

    private static RaceResultDraft BuildDraft(SessionStatusSnapshot status, TelemetrySnapshot? telemetry, RaceOutcome fallbackOutcome)
    {
        if (telemetry is null)
        {
            return new RaceResultDraft
            {
                LeagueId = status.LeagueId,
                LeagueName = status.LeagueName,
                TrackName = status.TrackName,
                CompletedUtc = DateTime.UtcNow,
                Outcome = fallbackOutcome,
                Confidence = ResultConfidence.Low,
                Summary = fallbackOutcome == RaceOutcome.Abandoned
                    ? "Session ended before a finish state was confirmed. Manual confirmation required."
                    : "Telemetry was missing at session end. Manual confirmation required."
            };
        }

        var outcome = ResolveOutcome(telemetry, fallbackOutcome);
        var confidence = status.ForceManualReview || outcome != RaceOutcome.Finished
            ? ResultConfidence.Medium
            : telemetry.CompletedLaps >= 3
                ? ResultConfidence.High
                : ResultConfidence.Low;

        var actionText = outcome switch
        {
            RaceOutcome.Finished => "finished",
            RaceOutcome.Retired => "retired",
            RaceOutcome.Abandoned => "abandoned",
            _ => "ended"
        };

        var summary = $"{status.LeagueName} {actionText} at {status.TrackName}: P{telemetry.OverallPosition}/{telemetry.Entrants}, " +
                      $"class P{telemetry.ClassPosition}, {telemetry.CompletedLaps}/{Math.Max(telemetry.TotalLaps, telemetry.CompletedLaps)} laps.";

        return new RaceResultDraft
        {
            LeagueId = status.LeagueId,
            LeagueName = status.LeagueName,
            TrackName = status.TrackName,
            CompletedUtc = DateTime.UtcNow,
            Outcome = outcome,
            OverallPosition = telemetry.OverallPosition,
            ClassPosition = telemetry.ClassPosition,
            Entrants = telemetry.Entrants,
            LapsCompleted = telemetry.CompletedLaps,
            IsCleanRace = telemetry.WasCleanLap,
            Confidence = confidence,
            Summary = summary
        };
    }

    private static RaceOutcome ResolveOutcome(TelemetrySnapshot telemetry, RaceOutcome fallbackOutcome)
    {
        if (fallbackOutcome == RaceOutcome.Abandoned)
        {
            return RaceOutcome.Abandoned;
        }

        if (telemetry.ParticipantRaceState is >= 4)
        {
            return RaceOutcome.Retired;
        }

        if (telemetry.TotalLaps > 0 && telemetry.CompletedLaps < telemetry.TotalLaps && telemetry.ParticipantRaceState is 3 or 4)
        {
            return RaceOutcome.Retired;
        }

        return RaceOutcome.Finished;
    }
}
