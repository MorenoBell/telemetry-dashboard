using Telemetry.Core.Models;

namespace Telemetry.Core.Services;

public interface ITelemetryReader
{
    IReadOnlyList<string> GetAvailableLapIds();
    Task<TelemetryLap?> ReadLapAsync(string lapId, CancellationToken cancellationToken = default);
    Task<LapSummary?> ReadLapSummaryAsync(string lapId, CancellationToken cancellationToken = default);
}
