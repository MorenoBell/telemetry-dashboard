using Telemetry.Core.Services;

namespace Telemetry.Api;

public static class TelemetryEndpoints
{
    public static RouteGroupBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/telemetry").WithTags("Telemetry");

        group.MapGet("/", (ITelemetryReader reader) =>
        {
            var files = reader.GetAvailableFiles();
            return Results.Ok(files);
        });

        group.MapGet("/{fileName}/points", async (string fileName, ITelemetryReader reader, CancellationToken cancellationToken) =>
        {
            var points = await reader.ReadTelemetryAsync(fileName, cancellationToken);
            return points.Count == 0
                ? Results.NotFound(new { message = $"Telemetry file '{fileName}' was not found or was empty." })
                : Results.Ok(points);
        });

        group.MapGet("/{fileName}/laps", async (string fileName, ITelemetryReader reader, CancellationToken cancellationToken) =>
        {
            var summaries = await reader.ReadLapSummariesAsync(fileName, cancellationToken);
            return summaries.Count == 0
                ? Results.NotFound(new { message = $"Telemetry file '{fileName}' was not found or was empty." })
                : Results.Ok(summaries);
        });

        return group;
    }
}
