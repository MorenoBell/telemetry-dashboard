using System.Globalization;
using Telemetry.Core.Models;

namespace Telemetry.Core.Services;

public sealed class CsvTelemetryReader : ITelemetryReader
{
    private static readonly string[] SupportedExtensions = [".csv"];

    private readonly string _samplesDirectory;

    public CsvTelemetryReader(string samplesDirectory)
    {
        _samplesDirectory = samplesDirectory;
    }

    public IReadOnlyList<string> GetAvailableFiles()
    {
        if (!Directory.Exists(_samplesDirectory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(_samplesDirectory)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Select(path => Path.GetFileName(path)!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<TelemetryPoint>> ReadTelemetryAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var filePath = ResolveFilePath(fileName);

        if (!File.Exists(filePath))
        {
            return [];
        }

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length <= 1)
        {
            return [];
        }

        var headers = SplitCsvLine(lines[0]);
        var headerIndex = headers
            .Select((header, index) => new KeyValuePair<string, int>(Normalize(header), index))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        var points = new List<TelemetryPoint>();

        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = SplitCsvLine(line);
            points.Add(new TelemetryPoint(
                Sample: ReadInt(columns, headerIndex, lineIndex, "sample", "index"),
                TimeSeconds: ReadDouble(columns, headerIndex, lineIndex, "time", "timeseconds", "seconds", "elapsedtime"),
                SpeedKph: ReadDouble(columns, headerIndex, lineIndex, "speed", "speedkph", "speedkmh"),
                Throttle: ReadDouble(columns, headerIndex, lineIndex, "throttle", "throttlepct", "throttlepercent"),
                Brake: ReadDouble(columns, headerIndex, lineIndex, "brake", "brakepct", "brakepercent"),
                Rpm: ReadInt(columns, headerIndex, lineIndex, "rpm", "enginerpm"),
                Gear: ReadInt(columns, headerIndex, lineIndex, "gear")));
        }

        return points;
    }

    public async Task<IReadOnlyList<LapSummary>> ReadLapSummariesAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var points = await ReadTelemetryAsync(fileName, cancellationToken);
        if (points.Count == 0)
        {
            return [];
        }

        var lapDuration = points[^1].TimeSeconds <= 0 ? 0 : points[^1].TimeSeconds;
        var averageSpeed = points.Average(point => point.SpeedKph);
        var maxSpeed = points.Max(point => point.SpeedKph);

        return
        [
            new LapSummary(
                LapNumber: 1,
                Samples: points.Count,
                DurationSeconds: Math.Round(lapDuration, 3),
                AverageSpeedKph: Math.Round(averageSpeed, 2),
                MaxSpeedKph: Math.Round(maxSpeed, 2))
        ];
    }

    private string ResolveFilePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A telemetry file name is required.", nameof(fileName));
        }

        var safeFileName = Path.GetFileName(fileName);
        return Path.Combine(_samplesDirectory, safeFileName);
    }

    private static int ReadInt(
        IReadOnlyList<string> columns,
        IReadOnlyDictionary<string, int> headerIndex,
        int fallbackSample,
        params string[] candidateHeaders)
    {
        var value = ReadValue(columns, headerIndex, candidateHeaders);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallbackSample;
    }

    private static double ReadDouble(
        IReadOnlyList<string> columns,
        IReadOnlyDictionary<string, int> headerIndex,
        int fallbackSample,
        params string[] candidateHeaders)
    {
        var value = ReadValue(columns, headerIndex, candidateHeaders);
        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallbackSample;
    }

    private static string ReadValue(
        IReadOnlyList<string> columns,
        IReadOnlyDictionary<string, int> headerIndex,
        params string[] candidateHeaders)
    {
        foreach (var candidate in candidateHeaders)
        {
            if (!headerIndex.TryGetValue(candidate, out var index) || index >= columns.Count)
            {
                continue;
            }

            return columns[index];
        }

        return string.Empty;
    }

    private static string[] SplitCsvLine(string line)
    {
        return line.Split(',', StringSplitOptions.TrimEntries);
    }

    private static string Normalize(string header)
    {
        return new string(header.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}
