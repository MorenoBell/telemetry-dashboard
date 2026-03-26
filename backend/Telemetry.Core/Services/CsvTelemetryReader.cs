using System.Globalization;
using Telemetry.Core.Models;

namespace Telemetry.Core.Services;

public sealed class CsvTelemetryReader : ITelemetryReader
{
    private static readonly string[] RequiredHeaders =
    [
        "time",
        "distance",
        "speed",
        "throttle",
        "brake",
        "rpm",
        "gear",
        "latitude",
        "longitude"
    ];

    private readonly string _samplesDirectory;

    public CsvTelemetryReader(string samplesDirectory)
    {
        _samplesDirectory = samplesDirectory;
    }

    public IReadOnlyList<string> GetAvailableLapIds()
    {
        if (!Directory.Exists(_samplesDirectory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(_samplesDirectory, "*.csv")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray()!;
    }

    public async Task<TelemetryLap?> ReadLapAsync(string lapId, CancellationToken cancellationToken = default)
    {
        var filePath = ResolveFilePath(lapId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length == 0)
        {
            return new TelemetryLap(lapId, Path.GetFileName(filePath), [], [new TelemetryValidationError(1, "header", "The CSV file is empty.")]);
        }

        var headers = SplitCsvLine(lines[0]);
        var headerIndex = headers
            .Select((header, index) => new KeyValuePair<string, int>(Normalize(header), index))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        var errors = ValidateHeaders(headerIndex);
        var points = new List<TelemetryPoint>();

        if (errors.Count == 0)
        {
            for (var lineNumber = 2; lineNumber <= lines.Length; lineNumber++)
            {
                var rawLine = lines[lineNumber - 1];
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                var columns = SplitCsvLine(rawLine);
                var point = TryParsePoint(columns, headerIndex, lineNumber, errors);
                if (point is not null)
                {
                    points.Add(point);
                }
            }
        }

        return new TelemetryLap(lapId, Path.GetFileName(filePath), points, errors);
    }

    public async Task<LapSummary?> ReadLapSummaryAsync(string lapId, CancellationToken cancellationToken = default)
    {
        var lap = await ReadLapAsync(lapId, cancellationToken);
        if (lap is null || lap.ValidationErrors.Count > 0 || lap.Points.Count == 0)
        {
            return null;
        }

        return new LapSummary(
            LapId: lap.Id,
            SampleCount: lap.Points.Count,
            MaxSpeed: Math.Round(lap.Points.Max(point => point.Speed), 2),
            AverageSpeed: Math.Round(lap.Points.Average(point => point.Speed), 2),
            MaxBrake: Math.Round(lap.Points.Max(point => point.Brake), 2),
            AverageThrottle: Math.Round(lap.Points.Average(point => point.Throttle), 2));
    }

    private string ResolveFilePath(string lapId)
    {
        if (string.IsNullOrWhiteSpace(lapId))
        {
            throw new ArgumentException("A lap id is required.", nameof(lapId));
        }

        var safeLapId = Path.GetFileNameWithoutExtension(lapId);
        return Path.Combine(_samplesDirectory, $"{safeLapId}.csv");
    }

    private static List<TelemetryValidationError> ValidateHeaders(IReadOnlyDictionary<string, int> headerIndex)
    {
        var errors = new List<TelemetryValidationError>();

        foreach (var requiredHeader in RequiredHeaders)
        {
            if (!headerIndex.ContainsKey(requiredHeader))
            {
                errors.Add(new TelemetryValidationError(1, requiredHeader, $"Missing required column '{requiredHeader}'."));
            }
        }

        return errors;
    }

    private static TelemetryPoint? TryParsePoint(
        IReadOnlyList<string> columns,
        IReadOnlyDictionary<string, int> headerIndex,
        int lineNumber,
        ICollection<TelemetryValidationError> errors)
    {
        if (!TryReadDouble(columns, headerIndex, lineNumber, "time", errors, out var time) ||
            !TryReadDouble(columns, headerIndex, lineNumber, "distance", errors, out var distance) ||
            !TryReadDouble(columns, headerIndex, lineNumber, "speed", errors, out var speed) ||
            !TryReadDouble(columns, headerIndex, lineNumber, "throttle", errors, out var throttle) ||
            !TryReadDouble(columns, headerIndex, lineNumber, "brake", errors, out var brake) ||
            !TryReadInt(columns, headerIndex, lineNumber, "rpm", errors, out var rpm) ||
            !TryReadInt(columns, headerIndex, lineNumber, "gear", errors, out var gear) ||
            !TryReadDouble(columns, headerIndex, lineNumber, "latitude", errors, out var latitude) ||
            !TryReadDouble(columns, headerIndex, lineNumber, "longitude", errors, out var longitude))
        {
            return null;
        }

        return new TelemetryPoint(time, distance, speed, throttle, brake, rpm, gear, latitude, longitude);
    }

    private static bool TryReadDouble(
        IReadOnlyList<string> columns,
        IReadOnlyDictionary<string, int> headerIndex,
        int lineNumber,
        string columnName,
        ICollection<TelemetryValidationError> errors,
        out double value)
    {
        value = default;

        if (!TryGetColumnValue(columns, headerIndex, lineNumber, columnName, errors, out var rawValue))
        {
            return false;
        }

        if (double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        errors.Add(new TelemetryValidationError(lineNumber, columnName, $"'{rawValue}' is not a valid number."));
        return false;
    }

    private static bool TryReadInt(
        IReadOnlyList<string> columns,
        IReadOnlyDictionary<string, int> headerIndex,
        int lineNumber,
        string columnName,
        ICollection<TelemetryValidationError> errors,
        out int value)
    {
        value = default;

        if (!TryGetColumnValue(columns, headerIndex, lineNumber, columnName, errors, out var rawValue))
        {
            return false;
        }

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        errors.Add(new TelemetryValidationError(lineNumber, columnName, $"'{rawValue}' is not a valid integer."));
        return false;
    }

    private static bool TryGetColumnValue(
        IReadOnlyList<string> columns,
        IReadOnlyDictionary<string, int> headerIndex,
        int lineNumber,
        string columnName,
        ICollection<TelemetryValidationError> errors,
        out string value)
    {
        value = string.Empty;

        var columnIndex = headerIndex[columnName];
        if (columnIndex >= columns.Count)
        {
            errors.Add(new TelemetryValidationError(lineNumber, columnName, $"Column '{columnName}' is missing a value."));
            return false;
        }

        value = columns[columnIndex];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        errors.Add(new TelemetryValidationError(lineNumber, columnName, $"Column '{columnName}' is empty."));
        return false;
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
