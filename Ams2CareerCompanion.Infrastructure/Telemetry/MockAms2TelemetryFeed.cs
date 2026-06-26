using Ams2CareerCompanion.Core.Interfaces;
using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Infrastructure.Telemetry;

public sealed class MockAms2TelemetryFeed : IGameTelemetryFeed, IMockRaceSimulator
{
    private CancellationTokenSource? _simulationCts;

    public event EventHandler<SessionStatusSnapshot>? SessionStatusChanged;
    public event EventHandler<TelemetrySnapshot>? TelemetryReceived;

    public TelemetryConnectionState ConnectionState { get; private set; } = TelemetryConnectionState.Disconnected;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _simulationCts?.Cancel();
        _simulationCts?.Dispose();
        _simulationCts = null;
        ConnectionState = TelemetryConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    public async Task RunSimulationAsync(MockRaceScenario scenario, CancellationToken cancellationToken = default)
    {
        _simulationCts?.Cancel();
        _simulationCts?.Dispose();
        _simulationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _simulationCts.Token;

        ConnectionState = TelemetryConnectionState.Simulating;

        SessionStatusChanged?.Invoke(this, new SessionStatusSnapshot
        {
            IsConnected = true,
            SessionType = SessionType.Race,
            SessionPhase = SessionPhase.Grid,
            LeagueId = scenario.LeagueId,
            LeagueName = scenario.LeagueName,
            TrackName = scenario.TrackName,
            ForceManualReview = scenario.ForceManualReview
        });

        await Task.Delay(600, token);

        SessionStatusChanged?.Invoke(this, new SessionStatusSnapshot
        {
            IsConnected = true,
            SessionType = SessionType.Race,
            SessionPhase = SessionPhase.Running,
            LeagueId = scenario.LeagueId,
            LeagueName = scenario.LeagueName,
            TrackName = scenario.TrackName,
            ForceManualReview = scenario.ForceManualReview
        });

        var startPosition = Math.Min(scenario.GridSize, Math.Max(scenario.PlannedFinishPosition + Random.Shared.Next(2, 5), scenario.PlannedFinishPosition + 1));

        for (var lap = 1; lap <= scenario.TotalLaps; lap++)
        {
            token.ThrowIfCancellationRequested();

            var progress = scenario.TotalLaps == 1 ? 1d : (lap - 1d) / (scenario.TotalLaps - 1d);
            var position = (int)Math.Round(startPosition + (scenario.PlannedFinishPosition - startPosition) * progress);
            position = Math.Clamp(position, 1, scenario.GridSize);

            TelemetryReceived?.Invoke(this, new TelemetrySnapshot
            {
                TimestampUtc = DateTime.UtcNow,
                CurrentLap = lap,
                CompletedLaps = lap,
                TotalLaps = scenario.TotalLaps,
                OverallPosition = position,
                ClassPosition = Math.Min(position, scenario.PlannedClassPosition),
                Entrants = scenario.GridSize,
                FuelLiters = Math.Max(3, 42 - lap * 4.2),
                IsInPit = false,
                WasCleanLap = scenario.IsCleanRace,
                ParticipantRaceState = 2
            });

            await Task.Delay(650, token);
        }

        TelemetryReceived?.Invoke(this, new TelemetrySnapshot
        {
            TimestampUtc = DateTime.UtcNow,
            CurrentLap = scenario.TotalLaps,
            CompletedLaps = scenario.TotalLaps,
            TotalLaps = scenario.TotalLaps,
            OverallPosition = scenario.PlannedFinishPosition,
            ClassPosition = scenario.PlannedClassPosition,
            Entrants = scenario.GridSize,
            FuelLiters = 6,
            IsInPit = false,
            WasCleanLap = scenario.IsCleanRace,
            ParticipantRaceState = 3
        });

        SessionStatusChanged?.Invoke(this, new SessionStatusSnapshot
        {
            IsConnected = true,
            SessionType = SessionType.Race,
            SessionPhase = SessionPhase.Finished,
            LeagueId = scenario.LeagueId,
            LeagueName = scenario.LeagueName,
            TrackName = scenario.TrackName,
            ForceManualReview = scenario.ForceManualReview
        });

        await Task.Delay(450, token);

        ConnectionState = TelemetryConnectionState.Disconnected;
        SessionStatusChanged?.Invoke(this, new SessionStatusSnapshot
        {
            IsConnected = false,
            SessionType = SessionType.None,
            SessionPhase = SessionPhase.Idle
        });
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
