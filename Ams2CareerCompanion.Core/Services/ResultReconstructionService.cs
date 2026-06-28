using Ams2CareerCompanion.Core.Interfaces;
using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Core.Services;

public sealed class ResultReconstructionService
{
    private readonly RaceAutomationRunContext _runContext;
    private SessionStatusSnapshot _lastStatus = new();
    private TelemetrySnapshot? _lastTelemetry;
    private bool _raceSessionActive;
    private bool _resultEmittedForCurrentSession;
    private bool _sawRunningPhase;

    public event EventHandler<RaceResultDraft>? DraftCreated;

    public ResultReconstructionService(IGameTelemetryFeed telemetryFeed, RaceAutomationRunContext runContext)
    {
        _runContext = runContext;
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
            if (_raceSessionActive &&
                !_resultEmittedForCurrentSession &&
                _sawRunningPhase &&
                _lastStatus.SessionType == SessionType.Race &&
                _lastStatus.SessionPhase == SessionPhase.Running &&
                status.SessionPhase == SessionPhase.Grid)
            {
                _resultEmittedForCurrentSession = true;
                DraftCreated?.Invoke(this, BuildDraft(_lastStatus, _lastTelemetry, RaceOutcome.Restarted, _runContext.CurrentRunId));
            }

            _raceSessionActive = true;
            _sawRunningPhase |= status.SessionPhase == SessionPhase.Running;
        }

        if (_raceSessionActive && !_resultEmittedForCurrentSession && _lastStatus.SessionPhase == SessionPhase.Running && status.SessionPhase == SessionPhase.Finished)
        {
            _resultEmittedForCurrentSession = true;
            DraftCreated?.Invoke(this, BuildDraft(status, _lastTelemetry, RaceOutcome.Finished, _runContext.CurrentRunId));
        }

        if (_raceSessionActive &&
            !_resultEmittedForCurrentSession &&
            _sawRunningPhase &&
            _lastStatus.SessionType == SessionType.Race &&
            _lastStatus.SessionPhase == SessionPhase.Running &&
            status.SessionType != SessionType.Race)
        {
            _resultEmittedForCurrentSession = true;
            DraftCreated?.Invoke(this, BuildDraft(_lastStatus, _lastTelemetry, RaceOutcome.Abandoned, _runContext.CurrentRunId));
        }

        if (status.SessionType == SessionType.None && status.SessionPhase == SessionPhase.Idle)
        {
            if (_raceSessionActive && !_resultEmittedForCurrentSession && _sawRunningPhase)
            {
                _resultEmittedForCurrentSession = true;
                DraftCreated?.Invoke(this, BuildDraft(_lastStatus, _lastTelemetry, RaceOutcome.Abandoned, _runContext.CurrentRunId));
            }

            _raceSessionActive = false;
            _resultEmittedForCurrentSession = false;
            _sawRunningPhase = false;
            _lastTelemetry = null;
        }

        _lastStatus = status;
    }

    private static RaceResultDraft BuildDraft(SessionStatusSnapshot status, TelemetrySnapshot? telemetry, RaceOutcome fallbackOutcome, Guid? automationRunId)
    {
        if (telemetry is null)
        {
            return new RaceResultDraft
            {
                AutomationRunId = automationRunId,
                LeagueId = status.LeagueId,
                LeagueName = status.LeagueName,
                TrackName = status.TrackName,
                CompletedUtc = DateTime.UtcNow,
                Outcome = fallbackOutcome,
                Confidence = ResultConfidence.Low,
                ValidationNotes = [fallbackOutcome == RaceOutcome.Abandoned
                    ? "Session ended before a finish state was confirmed."
                    : "Telemetry was missing at session end."],
                Summary = fallbackOutcome == RaceOutcome.Abandoned
                    ? "Manual review needed. Result could not be confirmed automatically."
                    : "Manual review needed. Result could not be confirmed automatically."
            };
        }

        var outcome = ResolveOutcome(telemetry, fallbackOutcome);
        var validationNotes = BuildValidationNotes(telemetry);
        var confidence = validationNotes.Count > 0 || telemetry.OverallPosition <= 0 || telemetry.ClassPosition <= 0
            ? ResultConfidence.Low
            : outcome == RaceOutcome.Finished
                ? telemetry.CompletedLaps >= 3
                    ? ResultConfidence.High
                    : ResultConfidence.Medium
                : ResultConfidence.Medium;

        if (status.ForceManualReview && confidence == ResultConfidence.High)
        {
            confidence = ResultConfidence.Medium;
        }

        var actionText = outcome switch
        {
            RaceOutcome.Finished => "finished",
            RaceOutcome.Disqualified => "was disqualified",
            RaceOutcome.Retired => "retired",
            RaceOutcome.Restarted => "was restarted before completion",
            RaceOutcome.Abandoned => "was abandoned before completion",
            _ => "ended"
        };

        var summaryPrefix = outcome == RaceOutcome.Finished
            ? string.Empty
            : outcome == RaceOutcome.Disqualified
                ? "Incident flagged. "
                : outcome == RaceOutcome.Retired
                    ? "Incident flagged. "
                    : outcome == RaceOutcome.Restarted
                        ? "Session restarted before completion. "
                        : outcome == RaceOutcome.Abandoned
                            ? "Session ended early. "
                            : string.Empty;

        var summary = $"{summaryPrefix}{status.LeagueName} {actionText} at {status.TrackName}: P{telemetry.OverallPosition}/{telemetry.Entrants}, " +
                      $"class P{telemetry.ClassPosition}, {telemetry.CompletedLaps}/{Math.Max(telemetry.TotalLaps, telemetry.CompletedLaps)} laps.";

        if (validationNotes.Count > 0)
        {
            summary = $"Manual review needed. {summary} Telemetry validation: {string.Join("; ", validationNotes)}.";
        }
        else if (status.ForceManualReview && outcome == RaceOutcome.Finished)
        {
            summary = $"Manual review needed. {summary}";
        }

        return new RaceResultDraft
        {
            AutomationRunId = automationRunId,
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
            ValidationNotes = validationNotes,
            Summary = summary
        };
    }

    private static RaceOutcome ResolveOutcome(TelemetrySnapshot telemetry, RaceOutcome fallbackOutcome)
    {
        if (fallbackOutcome == RaceOutcome.Abandoned)
        {
            return RaceOutcome.Abandoned;
        }

        if (fallbackOutcome == RaceOutcome.Restarted)
        {
            return RaceOutcome.Restarted;
        }

        if (telemetry.ParticipantRaceState >= 5)
        {
            return RaceOutcome.Disqualified;
        }

        if (telemetry.ParticipantRaceState == 4)
        {
            return RaceOutcome.Retired;
        }

        if (telemetry.TotalLaps > 0 && telemetry.CompletedLaps < telemetry.TotalLaps && telemetry.ParticipantRaceState is 3 or 4)
        {
            return RaceOutcome.Retired;
        }

        return RaceOutcome.Finished;
    }

    private static List<string> BuildValidationNotes(TelemetrySnapshot telemetry)
    {
        var notes = new List<string>();

        if (telemetry.Entrants <= 0)
        {
            notes.Add("entrant count is missing");
        }

        if (telemetry.OverallPosition <= 0 || (telemetry.Entrants > 0 && telemetry.OverallPosition > telemetry.Entrants))
        {
            notes.Add("overall position is outside the field size");
        }

        if (telemetry.ClassPosition <= 0 || (telemetry.Entrants > 0 && telemetry.ClassPosition > telemetry.Entrants))
        {
            notes.Add("class position is outside the field size");
        }

        if (telemetry.CompletedLaps < 0)
        {
            notes.Add("completed lap count is negative");
        }

        if (telemetry.TotalLaps > 0 && telemetry.CompletedLaps > telemetry.TotalLaps)
        {
            notes.Add("completed laps exceed total laps");
        }

        return notes;
    }
}
