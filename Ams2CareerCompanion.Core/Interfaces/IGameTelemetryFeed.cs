using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Core.Interfaces;

public interface IGameTelemetryFeed : IAsyncDisposable
{
    event EventHandler<SessionStatusSnapshot>? SessionStatusChanged;
    event EventHandler<TelemetrySnapshot>? TelemetryReceived;

    TelemetryConnectionState ConnectionState { get; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public interface IMockRaceSimulator
{
    Task RunSimulationAsync(MockRaceScenario scenario, CancellationToken cancellationToken = default);
}
