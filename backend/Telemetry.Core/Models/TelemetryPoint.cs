namespace Telemetry.Core.Models;

public sealed record TelemetryPoint(
    double Time,
    double Distance,
    double Speed,
    double Throttle,
    double Brake,
    int Rpm,
    int Gear,
    double Latitude,
    double Longitude);
