using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text;
using Ams2CareerCompanion.Core.Interfaces;
using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Infrastructure.Telemetry;

public sealed class Ams2SharedMemoryFeed : IGameTelemetryFeed
{
    private const int SharedMemoryVersion = 14;
    private const int StoredParticipantsMax = 64;
    private const int ParticipantInfoSize = 100;
    private const int OffsetVersion = 0;
    private const int OffsetGameState = 8;
    private const int OffsetSessionState = 12;
    private const int OffsetRaceState = 16;
    private const int OffsetViewedParticipantIndex = 20;
    private const int OffsetNumParticipants = 24;
    private const int OffsetParticipantInfo = 28;
    private const int ParticipantOffsetRacePosition = 84;
    private const int ParticipantOffsetLapsCompleted = 88;
    private const int ParticipantOffsetCurrentLap = 92;
    private const int OffsetCarName = 6444;
    private const int OffsetCarClassName = 6508;
    private const int OffsetLapsInEvent = 6572;
    private const int OffsetTrackLocation = 6576;
    private const int OffsetTrackVariation = 6640;
    private const int OffsetLapInvalidated = 6712;
    private const int OffsetLastLapTime = 6720;
    private const int OffsetCurrentTime = 6724;
    private const int OffsetEventTimeRemaining = 6740;
    private const int OffsetPitMode = 6808;
    private const int OffsetFuelLevel = 6840;
    private const int OffsetFuelCapacity = 6844;
    private const int OffsetSequenceNumber = 7320;
    private const int OffsetLastLapTimesByParticipant = 9456;
    private const int OffsetLapsInvalidatedByParticipant = 9520;
    private const int OffsetRaceStatesByParticipant = 9584;
    private const int OffsetPitModesByParticipant = 9840;
    private const int OffsetCarClassNamesByParticipant = 19312;
    private const int MinimumBufferSize = 23408;

    private static readonly string[] MapNames = ["$pcars2$", @"Local\$pcars2$", @"Global\$pcars2$"];

    private readonly string? _logPath;
    private MemoryMappedFile? _mappedFile;
    private MemoryMappedViewAccessor? _viewAccessor;
    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;
    private SessionStatusSnapshot? _lastSessionStatus;
    private string? _lastDisconnectReason;

    public Ams2SharedMemoryFeed(string? diagnosticsDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(diagnosticsDirectory))
        {
            Directory.CreateDirectory(diagnosticsDirectory);
            _logPath = Path.Combine(diagnosticsDirectory, "telemetry.log");
        }
    }

    public event EventHandler<SessionStatusSnapshot>? SessionStatusChanged;
    public event EventHandler<TelemetrySnapshot>? TelemetryReceived;

    public TelemetryConnectionState ConnectionState { get; private set; } = TelemetryConnectionState.Disconnected;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_pollTask is not null)
        {
            return Task.CompletedTask;
        }

        Log("Starting AMS2 shared memory polling.");
        _pollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pollTask = Task.Run(() => PollLoopAsync(_pollCts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_pollCts is null)
        {
            return;
        }

        _pollCts.Cancel();

        if (_pollTask is not null)
        {
            try
            {
                await _pollTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _pollCts.Dispose();
        _pollCts = null;
        _pollTask = null;
        CloseMap();
        UpdateConnectionState(TelemetryConnectionState.Disconnected);
        Log("Stopped AMS2 shared memory polling.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[MinimumBufferSize];

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                EnsureMapOpened();

                if (_viewAccessor is null)
                {
                    PublishDisconnectedState(BuildMapNotFoundReason());
                    await Task.Delay(500, cancellationToken);
                    continue;
                }

                if (!TryReadConsistentBuffer(buffer, out var readFailureReason, out var isTransientBusy))
                {
                    if (!isTransientBusy)
                    {
                        PublishDisconnectedState(readFailureReason);
                        await Task.Delay(250, cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(50, cancellationToken);
                    }

                    continue;
                }

                var status = BuildSessionStatus(buffer);
                PublishSessionStatus(status);

                if (status.SessionType == SessionType.Race && status.SessionPhase is SessionPhase.Grid or SessionPhase.Running or SessionPhase.Finished)
                {
                    var telemetry = BuildTelemetrySnapshot(buffer);
                    TelemetryReceived?.Invoke(this, telemetry);
                }

                await Task.Delay(125, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Log($"Telemetry poll exception: {ex}");
                CloseMap();
                PublishDisconnectedState("Telemetry reader faulted. See telemetry.log.");
                await Task.Delay(750, cancellationToken);
            }
        }
    }

    private void EnsureMapOpened()
    {
        if (_mappedFile is not null && _viewAccessor is not null)
        {
            return;
        }

        foreach (var mapName in MapNames)
        {
            try
            {
                _mappedFile = MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.Read);
                _viewAccessor = _mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                UpdateConnectionState(TelemetryConnectionState.Monitoring);
                Log($"Opened shared memory map '{mapName}'.");
                return;
            }
            catch (FileNotFoundException)
            {
            }
            catch (UnauthorizedAccessException)
            {
                Log($"Access denied opening shared memory map '{mapName}'.");
            }
        }
    }

    private bool TryReadConsistentBuffer(byte[] buffer, out string readFailureReason, out bool isTransientBusy)
    {
        readFailureReason = "Shared memory read failed.";
        isTransientBusy = false;

        if (_viewAccessor is null)
        {
            readFailureReason = BuildMapNotFoundReason();
            return false;
        }

        var before = ReadUInt32(_viewAccessor, OffsetSequenceNumber);
        if ((before & 1) == 1)
        {
            readFailureReason = "AMS2 is updating shared memory. Retrying.";
            isTransientBusy = true;
            return false;
        }

        _viewAccessor.ReadArray(0, buffer, 0, buffer.Length);
        var after = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(OffsetSequenceNumber, 4));

        if (before != after || (after & 1) == 1)
        {
            readFailureReason = "Shared memory updated during read. Retrying.";
            isTransientBusy = true;
            return false;
        }

        var version = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(OffsetVersion, 4));
        if (version != SharedMemoryVersion)
        {
            readFailureReason = $"Shared memory version mismatch. Expected {SharedMemoryVersion}, got {version}.";
            Log(readFailureReason);
            return false;
        }

        return true;
    }

    private SessionStatusSnapshot BuildSessionStatus(byte[] buffer)
    {
        var sessionState = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(OffsetSessionState, 4));
        var raceState = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(OffsetRaceState, 4));
        var gameState = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(OffsetGameState, 4));

        var sessionType = sessionState switch
        {
            1 => SessionType.Practice,
            3 => SessionType.Qualifying,
            5 => SessionType.Race,
            4 => SessionType.Race,
            _ => SessionType.None
        };

        var sessionPhase = raceState switch
        {
            1 => SessionPhase.Grid,
            2 => SessionPhase.Running,
            3 or 4 or 5 or 6 => SessionPhase.Finished,
            _ => sessionType == SessionType.Race ? SessionPhase.Grid : SessionPhase.Idle
        };

        if (gameState is 0 or 1)
        {
            sessionType = SessionType.None;
            sessionPhase = SessionPhase.Idle;
        }

        var location = ReadString(buffer, OffsetTrackLocation, 64);
        var variation = ReadString(buffer, OffsetTrackVariation, 64);
        var trackName = string.IsNullOrWhiteSpace(variation) ? location : $"{location} - {variation}";

        return new SessionStatusSnapshot
        {
            IsConnected = true,
            SessionType = sessionType,
            SessionPhase = sessionPhase,
            LeagueId = string.Empty,
            LeagueName = ReadString(buffer, OffsetCarClassName, 64),
            TrackName = trackName
        };
    }

    private TelemetrySnapshot BuildTelemetrySnapshot(byte[] buffer)
    {
        var viewedIndex = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(OffsetViewedParticipantIndex, 4));
        var participantCount = Math.Clamp(BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(OffsetNumParticipants, 4)), 0, StoredParticipantsMax);
        var safeViewedIndex = viewedIndex >= 0 && viewedIndex < participantCount ? viewedIndex : 0;
        var participantOffset = OffsetParticipantInfo + safeViewedIndex * ParticipantInfoSize;

        var lapsCompleted = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(participantOffset + ParticipantOffsetLapsCompleted, 4));
        var currentLap = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(participantOffset + ParticipantOffsetCurrentLap, 4));
        var overallPosition = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(participantOffset + ParticipantOffsetRacePosition, 4));
        var pitMode = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(OffsetPitMode, 4));
        var lapInvalidated = buffer[OffsetLapInvalidated] != 0;

        if (safeViewedIndex < StoredParticipantsMax)
        {
            lapInvalidated = buffer[OffsetLapsInvalidatedByParticipant + safeViewedIndex] != 0;
        }

        var classPosition = BuildClassPosition(buffer, safeViewedIndex, participantCount, overallPosition);
        var fuelLevel = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(OffsetFuelLevel, 4));
        var fuelCapacity = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(OffsetFuelCapacity, 4));
        var lastLapTime = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(OffsetLastLapTime, 4));
        if (safeViewedIndex < StoredParticipantsMax)
        {
            var participantLastLapTime = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(OffsetLastLapTimesByParticipant + safeViewedIndex * 4, 4));
            if (participantLastLapTime > 0)
            {
                lastLapTime = participantLastLapTime;
            }
        }

        return new TelemetrySnapshot
        {
            TimestampUtc = DateTime.UtcNow,
            CurrentLap = (int)Math.Max(currentLap, lapsCompleted),
            CompletedLaps = (int)lapsCompleted,
            OverallPosition = (int)Math.Max(overallPosition, 1),
            ClassPosition = classPosition,
            Entrants = Math.Max(participantCount, 1),
            FuelLiters = fuelCapacity > 0 ? fuelCapacity * fuelLevel : 0,
            IsInPit = pitMode is 1 or 2 or 3 or 4 or 5,
            WasCleanLap = !lapInvalidated && lastLapTime >= 0
        };
    }

    private int BuildClassPosition(byte[] buffer, int viewedIndex, int participantCount, uint fallbackPosition)
    {
        var viewedClass = ReadString(buffer, OffsetCarClassNamesByParticipant + viewedIndex * 64, 64);
        if (string.IsNullOrWhiteSpace(viewedClass))
        {
            viewedClass = ReadString(buffer, OffsetCarClassName, 64);
        }

        if (string.IsNullOrWhiteSpace(viewedClass))
        {
            return (int)Math.Max(fallbackPosition, 1);
        }

        var classEntries = new List<(uint Position, int Index)>();
        for (var i = 0; i < participantCount; i++)
        {
            var className = ReadString(buffer, OffsetCarClassNamesByParticipant + i * 64, 64);
            if (!string.Equals(className, viewedClass, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var position = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(OffsetParticipantInfo + i * ParticipantInfoSize + ParticipantOffsetRacePosition, 4));
            if (position == 0)
            {
                continue;
            }

            classEntries.Add((position, i));
        }

        if (classEntries.Count == 0)
        {
            return (int)Math.Max(fallbackPosition, 1);
        }

        classEntries.Sort((a, b) => a.Position.CompareTo(b.Position));
        var idx = classEntries.FindIndex(x => x.Index == viewedIndex);
        return idx >= 0 ? idx + 1 : (int)Math.Max(fallbackPosition, 1);
    }

    private void PublishSessionStatus(SessionStatusSnapshot status)
    {
        if (_lastSessionStatus is not null &&
            _lastSessionStatus.SessionType == status.SessionType &&
            _lastSessionStatus.SessionPhase == status.SessionPhase &&
            string.Equals(_lastSessionStatus.TrackName, status.TrackName, StringComparison.Ordinal) &&
            string.Equals(_lastSessionStatus.LeagueName, status.LeagueName, StringComparison.Ordinal))
        {
            return;
        }

        _lastSessionStatus = status;
        SessionStatusChanged?.Invoke(this, status);
        Log($"Session state: connected={status.IsConnected}, type={status.SessionType}, phase={status.SessionPhase}, league='{status.LeagueName}', track='{status.TrackName}'.");
    }

    private void PublishDisconnectedState(string reason)
    {
        UpdateConnectionState(TelemetryConnectionState.Disconnected);
        var status = new SessionStatusSnapshot
        {
            IsConnected = false,
            SessionType = SessionType.None,
            SessionPhase = SessionPhase.Idle,
            TrackName = reason
        };

        if (!string.Equals(_lastDisconnectReason, reason, StringComparison.Ordinal))
        {
            _lastDisconnectReason = reason;
            Log($"Disconnected: {reason}");
        }

        PublishSessionStatus(status);
    }

    private void UpdateConnectionState(TelemetryConnectionState state)
    {
        ConnectionState = state;
    }

    private void CloseMap()
    {
        _viewAccessor?.Dispose();
        _mappedFile?.Dispose();
        _viewAccessor = null;
        _mappedFile = null;
    }

    private string BuildMapNotFoundReason()
    {
        return IsAms2Running()
            ? "AMS2 is running but shared memory was not found. Check AMS2 telemetry/shared-memory settings."
            : "AMS2 is not running.";
    }

    private static bool IsAms2Running()
    {
        return Process.GetProcessesByName("AMS2").Length > 0 || Process.GetProcessesByName("AMS2AVX").Length > 0;
    }

    private void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(_logPath))
        {
            return;
        }

        try
        {
            File.AppendAllText(_logPath, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private static uint ReadUInt32(MemoryMappedViewAccessor accessor, int offset)
    {
        accessor.Read(offset, out uint value);
        return value;
    }

    private static string ReadString(byte[] buffer, int offset, int length)
    {
        var slice = buffer.AsSpan(offset, length);
        var terminator = slice.IndexOf((byte)0);
        if (terminator >= 0)
        {
            slice = slice[..terminator];
        }

        return Encoding.ASCII.GetString(slice).Trim();
    }
}
