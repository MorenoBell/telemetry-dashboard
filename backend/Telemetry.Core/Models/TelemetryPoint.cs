namespace Telemetry.Core.Models;

public sealed record TelemetryPoint(
    int Sample,
    double TimeSeconds,
    double SpeedKph,
    double Throttle,
    double Brake,
    int Rpm,
    int Gear);
