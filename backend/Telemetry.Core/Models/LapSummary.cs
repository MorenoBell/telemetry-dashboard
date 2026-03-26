namespace Telemetry.Core.Models;

public sealed record LapSummary(
    string LapId,
    int SampleCount,
    double MaxSpeed,
    double AverageSpeed,
    double MaxBrake,
    double AverageThrottle);
