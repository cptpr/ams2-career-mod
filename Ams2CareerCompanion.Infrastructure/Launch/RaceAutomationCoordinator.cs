using Ams2CareerCompanion.Core.Interfaces;
using Ams2CareerCompanion.Core.Models;
using Ams2CareerCompanion.Core.Services;
using Ams2CareerCompanion.Infrastructure.Diagnostics;

namespace Ams2CareerCompanion.Infrastructure.Launch;

public sealed class RaceAutomationCoordinator : IDisposable
{
    private readonly RaceAutomationRunContext _runContext;
    private readonly IGameTelemetryFeed _telemetryFeed;
    private readonly Ams2LaunchService _launchService;
    private readonly ChampionshipEditorPresetExportAdapter _exportAdapter;
    private readonly RaceAutomationTraceWriter? _traceWriter;
    private RaceAutomationStatus _currentStatus = new()
    {
        Stage = RaceAutomationStage.Idle,
        Headline = "No race automation active.",
        Detail = "Generate or load a career event to prepare the next race."
    };

    public RaceAutomationCoordinator(
        IGameTelemetryFeed telemetryFeed,
        Ams2LaunchService launchService,
        ChampionshipEditorPresetExportAdapter exportAdapter,
        RaceAutomationRunContext runContext,
        RaceAutomationTraceWriter? traceWriter = null)
    {
        _runContext = runContext;
        _telemetryFeed = telemetryFeed;
        _launchService = launchService;
        _exportAdapter = exportAdapter;
        _traceWriter = traceWriter;

        _telemetryFeed.SessionStatusChanged += OnSessionStatusChanged;
        _telemetryFeed.TelemetryReceived += OnTelemetryReceived;
    }

    public event EventHandler<RaceAutomationStatus>? StatusChanged;

    public RaceAutomationStatus CurrentStatus => _currentStatus;

    public EventExportResult PrepareEvent(CareerEventPlan? eventPlan)
    {
        var run = _runContext.StartNewRun("event-prepared");
        var result = _exportAdapter.Apply(eventPlan);
        Publish(result.Success
            ? new RaceAutomationStatus
            {
                RunId = run.RunId,
                Stage = result.RequiresGameRestart ? RaceAutomationStage.RestartRequired : RaceAutomationStage.EventPrepared,
                Headline = result.RequiresGameRestart ? "Event prepared. Restart required." : "Event prepared.",
                Detail = result.Message,
                RequiresGameRestart = result.RequiresGameRestart
            }
            : new RaceAutomationStatus
            {
                RunId = run.RunId,
                Stage = RaceAutomationStage.Error,
                Headline = "Event preparation failed.",
                Detail = result.Message
            });
        return result;
    }

    public (EventExportResult Export, LaunchResult Launch) PrepareAndLaunch(CareerEventPlan? eventPlan, bool vr)
    {
        var exportResult = PrepareEvent(eventPlan);
        if (!exportResult.Success)
        {
            return (exportResult, new LaunchResult(false, false, exportResult.Message));
        }

        var launchResult = vr ? _launchService.LaunchVr() : _launchService.Launch();
        var run = _runContext.EnsureRun("launch-requested");
        Publish(launchResult.Success
            ? new RaceAutomationStatus
            {
                RunId = run.RunId,
                Stage = RaceAutomationStage.LaunchRequested,
                Headline = vr ? "Launch requested in VR." : "Launch requested.",
                Detail = exportResult.RequiresGameRestart
                    ? $"{exportResult.Message} AMS2 launch requested after restart-sensitive export."
                    : $"{exportResult.Message} AMS2 launch requested.",
                RequiresGameRestart = exportResult.RequiresGameRestart,
                LaunchRequested = launchResult.LaunchTriggered
            }
            : new RaceAutomationStatus
            {
                RunId = run.RunId,
                Stage = RaceAutomationStage.Error,
                Headline = "Launch failed.",
                Detail = launchResult.Message,
                RequiresGameRestart = exportResult.RequiresGameRestart
            });

        return (exportResult, launchResult);
    }

    private void OnSessionStatusChanged(object? sender, SessionStatusSnapshot snapshot)
    {
        RaceAutomationStatus status;

        if (!snapshot.IsConnected)
        {
            var shouldClearRun = _currentStatus.Stage is RaceAutomationStage.GridDetected or RaceAutomationStage.RaceRunning or RaceAutomationStage.SessionFinished;
            status = new RaceAutomationStatus
            {
                RunId = _runContext.CurrentRunId,
                Stage = _launchService.IsGameRunning ? RaceAutomationStage.WaitingForSession : RaceAutomationStage.Idle,
                Headline = _launchService.IsGameRunning ? "AMS2 running. Waiting for session feed." : "No live AMS2 session detected.",
                Detail = snapshot.TrackName
            };
            Publish(status);
            if (shouldClearRun)
            {
                _runContext.Clear();
            }
            return;
        }

        var run = snapshot.SessionPhase == SessionPhase.Grid && _currentStatus.Stage == RaceAutomationStage.RaceRunning
            ? _runContext.StartNewRun("session-restart")
            : _runContext.EnsureRun("live-session-detected");
        status = snapshot.SessionPhase switch
        {
            SessionPhase.Grid => new RaceAutomationStatus
            {
                RunId = run.RunId,
                Stage = RaceAutomationStage.GridDetected,
                Headline = "Grid detected.",
                Detail = $"{snapshot.LeagueName} at {snapshot.TrackName}"
            },
            SessionPhase.Running => new RaceAutomationStatus
            {
                RunId = run.RunId,
                Stage = RaceAutomationStage.RaceRunning,
                Headline = "Race running.",
                Detail = $"{snapshot.LeagueName} at {snapshot.TrackName}"
            },
            SessionPhase.Finished => new RaceAutomationStatus
            {
                RunId = run.RunId,
                Stage = RaceAutomationStage.SessionFinished,
                Headline = "Race finished.",
                Detail = $"Monitoring completed session at {snapshot.TrackName}."
            },
            _ => new RaceAutomationStatus
            {
                RunId = run.RunId,
                Stage = RaceAutomationStage.WaitingForSession,
                Headline = "AMS2 connected.",
                Detail = $"Monitoring {snapshot.TrackName}."
            }
        };

        Publish(status);
    }

    private void OnTelemetryReceived(object? sender, TelemetrySnapshot snapshot)
    {
        if (_currentStatus.Stage is not (RaceAutomationStage.GridDetected or RaceAutomationStage.RaceRunning or RaceAutomationStage.SessionFinished))
        {
            return;
        }

        Publish(new RaceAutomationStatus
        {
            RunId = _currentStatus.RunId ?? _runContext.CurrentRunId,
            Stage = _currentStatus.Stage,
            Headline = _currentStatus.Headline,
            Detail = _currentStatus.Detail,
            RequiresGameRestart = _currentStatus.RequiresGameRestart,
            LaunchRequested = _currentStatus.LaunchRequested,
            TelemetryLine =
                $"Overall P{snapshot.OverallPosition}/{snapshot.Entrants}  |  Class P{snapshot.ClassPosition}  |  " +
                $"Lap {Math.Max(snapshot.CurrentLap, 1)}  |  Completed {snapshot.CompletedLaps}  |  Fuel {snapshot.FuelLiters:0.0}L" +
                $"{(snapshot.IsInPit ? "  |  In Pit" : string.Empty)}"
        });
    }

    private void Publish(RaceAutomationStatus next)
    {
        _currentStatus = new RaceAutomationStatus
        {
            RunId = next.RunId,
            Stage = next.Stage,
            Headline = next.Headline,
            Detail = next.Detail,
            TelemetryLine = next.TelemetryLine,
            RequiresGameRestart = next.RequiresGameRestart,
            LaunchRequested = next.LaunchRequested,
            TimestampUtc = DateTime.UtcNow
        };
        _traceWriter?.Append(_currentStatus);
        StatusChanged?.Invoke(this, _currentStatus);
    }

    public void Dispose()
    {
        _telemetryFeed.SessionStatusChanged -= OnSessionStatusChanged;
        _telemetryFeed.TelemetryReceived -= OnTelemetryReceived;
    }
}
