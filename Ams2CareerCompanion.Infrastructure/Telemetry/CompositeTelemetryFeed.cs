using Ams2CareerCompanion.Core.Interfaces;
using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Infrastructure.Telemetry;

public sealed class CompositeTelemetryFeed : IGameTelemetryFeed, IMockRaceSimulator
{
    private readonly IGameTelemetryFeed _liveFeed;
    private readonly MockAms2TelemetryFeed _mockFeed;

    public CompositeTelemetryFeed(IGameTelemetryFeed liveFeed, MockAms2TelemetryFeed mockFeed)
    {
        _liveFeed = liveFeed;
        _mockFeed = mockFeed;

        _liveFeed.SessionStatusChanged += ForwardSessionStatusChanged;
        _liveFeed.TelemetryReceived += ForwardTelemetryReceived;
        _mockFeed.SessionStatusChanged += ForwardSessionStatusChanged;
        _mockFeed.TelemetryReceived += ForwardTelemetryReceived;
    }

    public event EventHandler<SessionStatusSnapshot>? SessionStatusChanged;
    public event EventHandler<TelemetrySnapshot>? TelemetryReceived;

    public TelemetryConnectionState ConnectionState
    {
        get
        {
            if (_mockFeed.ConnectionState == TelemetryConnectionState.Simulating)
            {
                return TelemetryConnectionState.Simulating;
            }

            return _liveFeed.ConnectionState != TelemetryConnectionState.Disconnected
                ? _liveFeed.ConnectionState
                : _mockFeed.ConnectionState;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _mockFeed.StartAsync(cancellationToken);
        await _liveFeed.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _liveFeed.StopAsync(cancellationToken);
        await _mockFeed.StopAsync(cancellationToken);
    }

    public Task RunSimulationAsync(MockRaceScenario scenario, CancellationToken cancellationToken = default)
    {
        return _mockFeed.RunSimulationAsync(scenario, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _liveFeed.SessionStatusChanged -= ForwardSessionStatusChanged;
        _liveFeed.TelemetryReceived -= ForwardTelemetryReceived;
        _mockFeed.SessionStatusChanged -= ForwardSessionStatusChanged;
        _mockFeed.TelemetryReceived -= ForwardTelemetryReceived;

        await _liveFeed.DisposeAsync();
        await _mockFeed.DisposeAsync();
    }

    private void ForwardSessionStatusChanged(object? sender, SessionStatusSnapshot snapshot)
    {
        SessionStatusChanged?.Invoke(this, snapshot);
    }

    private void ForwardTelemetryReceived(object? sender, TelemetrySnapshot snapshot)
    {
        TelemetryReceived?.Invoke(this, snapshot);
    }
}
