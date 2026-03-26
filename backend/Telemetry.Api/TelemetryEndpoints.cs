using Telemetry.Core.Services;

namespace Telemetry.Api;

public static class TelemetryEndpoints
{
    public static RouteGroupBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/telemetry").WithTags("Telemetry");

        group.MapGet("/laps", (ITelemetryReader reader) =>
        {
            var laps = reader.GetAvailableLapIds()
                .Select(id => new { id })
                .ToArray();

            return Results.Ok(laps);
        });

        group.MapGet("/lap/{id}", async (string id, ITelemetryReader reader, CancellationToken cancellationToken) =>
        {
            var lap = await reader.ReadLapAsync(id, cancellationToken);
            if (lap is null)
            {
                return Results.NotFound(new { message = $"Lap '{id}' was not found." });
            }

            return lap.ValidationErrors.Count > 0
                ? Results.UnprocessableEntity(new
                {
                    message = $"Lap '{id}' contains malformed telemetry rows.",
                    errors = lap.ValidationErrors
                })
                : Results.Ok(lap);
        });

        group.MapGet("/lap/{id}/summary", async (string id, ITelemetryReader reader, CancellationToken cancellationToken) =>
        {
            var lap = await reader.ReadLapAsync(id, cancellationToken);
            if (lap is null)
            {
                return Results.NotFound(new { message = $"Lap '{id}' was not found." });
            }

            if (lap.ValidationErrors.Count > 0)
            {
                return Results.UnprocessableEntity(new
                {
                    message = $"Lap '{id}' contains malformed telemetry rows.",
                    errors = lap.ValidationErrors
                });
            }

            var summary = await reader.ReadLapSummaryAsync(id, cancellationToken);
            return summary is null
                ? Results.NotFound(new { message = $"Lap '{id}' did not contain any telemetry samples." })
                : Results.Ok(summary);
        });

        return group;
    }
}
