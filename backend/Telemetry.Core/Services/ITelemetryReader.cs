using Telemetry.Core.Models;

namespace Telemetry.Core.Services;

public interface ITelemetryReader
{
    IReadOnlyList<string> GetAvailableFiles();
    Task<IReadOnlyList<TelemetryPoint>> ReadTelemetryAsync(string fileName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LapSummary>> ReadLapSummariesAsync(string fileName, CancellationToken cancellationToken = default);
}
