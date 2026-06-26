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
            var payload = JsonSerializer.Serialize(new RaceAutomationTraceRecord
            {
                TimestampUtc = status.TimestampUtc,
                Stage = status.Stage.ToString(),
                Headline = status.Headline,
                Detail = status.Detail,
                TelemetryLine = status.TelemetryLine,
                RequiresGameRestart = status.RequiresGameRestart,
                LaunchRequested = status.LaunchRequested
            }, JsonOptions);

            File.AppendAllText(_traceFilePath, payload + Environment.NewLine);
        }
        catch
        {
        }
    }

    private sealed class RaceAutomationTraceRecord
    {
        public DateTime TimestampUtc { get; init; }
        public string Stage { get; init; } = string.Empty;
        public string Headline { get; init; } = string.Empty;
        public string Detail { get; init; } = string.Empty;
        public string TelemetryLine { get; init; } = string.Empty;
        public bool RequiresGameRestart { get; init; }
        public bool LaunchRequested { get; init; }
    }
}
