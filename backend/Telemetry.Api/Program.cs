using Telemetry.Api;
using Telemetry.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var samplesDirectory = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "data", "samples"));
builder.Services.AddSingleton<ITelemetryReader>(_ => new CsvTelemetryReader(samplesDirectory));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.MapTelemetryEndpoints();

app.Run();
