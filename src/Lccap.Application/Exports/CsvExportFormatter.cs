using System.Globalization;
using System.Text;

namespace Lccap.Application.Exports;

/// <summary>Builds CSV text in memory with RFC4180-style quoting and CSV/formula injection mitigations.</summary>
public static class CsvExportFormatter
{
    /// <summary>Escapes a free-text cell: injection prefix, quoting, and doubled quotes.</summary>
    public static string EscapeTextCell(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var normalized = value;
        var first = normalized[0];
        if (first is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            normalized = $"'{normalized}";
        }

        var needsQuotes = normalized.IndexOfAny([',', '"', '\r', '\n']) >= 0;
        if (!needsQuotes)
        {
            return normalized;
        }

        var escaped = normalized.Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }

    public static string FormatDecimal(decimal? value, int scale = 2) =>
        value.HasValue
            ? value.Value.ToString($"F{scale}", CultureInfo.InvariantCulture)
            : string.Empty;

    public static string FormatDecimalRaw(decimal? value, int scale = 4) =>
        value.HasValue
            ? value.Value.ToString($"F{scale}", CultureInfo.InvariantCulture)
            : string.Empty;

    public static string FormatInstant(DateTimeOffset? value) =>
        value.HasValue
            ? value.Value.ToString("O", CultureInfo.InvariantCulture)
            : string.Empty;

    public static string FormatGuid(Guid value) => value.ToString("D", CultureInfo.InvariantCulture);

    public static string Build(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 128));
        sb.AppendLine(string.Join(",", headers));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row));
        }

        return sb.ToString();
    }
}
