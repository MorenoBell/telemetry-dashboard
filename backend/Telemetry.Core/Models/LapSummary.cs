namespace Telemetry.Core.Models;

public sealed record LapSummary(
    int LapNumber,
    int Samples,
    double DurationSeconds,
    double AverageSpeedKph,
    double MaxSpeedKph);
