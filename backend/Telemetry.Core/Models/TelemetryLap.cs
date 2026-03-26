namespace Telemetry.Core.Models;

public sealed record TelemetryLap(
    string Id,
    string FileName,
    IReadOnlyList<TelemetryPoint> Points,
    IReadOnlyList<TelemetryValidationError> ValidationErrors);
