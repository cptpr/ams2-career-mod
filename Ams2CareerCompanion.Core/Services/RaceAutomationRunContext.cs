namespace Ams2CareerCompanion.Core.Services;

public sealed class RaceAutomationRunContext
{
    private readonly object _gate = new();
    private Guid? _currentRunId;
    private string _origin = "idle";
    private DateTime? _startedUtc;

    public Guid? CurrentRunId
    {
        get
        {
            lock (_gate)
            {
                return _currentRunId;
            }
        }
    }

    public RaceAutomationRunSnapshot StartNewRun(string origin)
    {
        lock (_gate)
        {
            _currentRunId = Guid.NewGuid();
            _origin = origin;
            _startedUtc = DateTime.UtcNow;
            return BuildSnapshotUnsafe();
        }
    }

    public RaceAutomationRunSnapshot EnsureRun(string origin)
    {
        lock (_gate)
        {
            if (_currentRunId is null)
            {
                _currentRunId = Guid.NewGuid();
                _startedUtc = DateTime.UtcNow;
            }

            _origin = origin;
            return BuildSnapshotUnsafe();
        }
    }

    public RaceAutomationRunSnapshot Capture()
    {
        lock (_gate)
        {
            return BuildSnapshotUnsafe();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _currentRunId = null;
            _origin = "idle";
            _startedUtc = null;
        }
    }

    private RaceAutomationRunSnapshot BuildSnapshotUnsafe()
    {
        return new RaceAutomationRunSnapshot
        {
            RunId = _currentRunId,
            Origin = _origin,
            StartedUtc = _startedUtc
        };
    }
}

public sealed class RaceAutomationRunSnapshot
{
    public Guid? RunId { get; init; }
    public string Origin { get; init; } = "idle";
    public DateTime? StartedUtc { get; init; }
}
