namespace Telemetry.Core.Models;

public sealed record TelemetryValidationError(
    int LineNumber,
    string Column,
    string Message);
