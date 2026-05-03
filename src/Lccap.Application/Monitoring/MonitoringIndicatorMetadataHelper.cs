using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lccap.Application.Monitoring;

public static class MonitoringIndicatorMetadataHelper
{
    public sealed record ParsedMetadata(
        decimal? CurrentValue,
        decimal? ProgressPercent,
        string? Frequency,
        string? ResponsibleOffice);

    public static ParsedMetadata Parse(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new ParsedMetadata(null, null, null, null);
        }

        using var doc = JsonDocument.Parse(metadataJson);
        var r = doc.RootElement;
        if (r.ValueKind != JsonValueKind.Object)
        {
            return new ParsedMetadata(null, null, null, null);
        }

        return new ParsedMetadata(
            TryGetDecimal(r, "currentValue"),
            TryGetDecimal(r, "progressPercent"),
            TryGetString(r, "frequency"),
            TryGetString(r, "responsibleOffice"));
    }

    public static string Merge(
        string existingJson,
        decimal? currentValue,
        decimal? progressPercent,
        string? frequency,
        string? responsibleOffice)
    {
        var root = ParseObject(existingJson);
        SetDecimal(root, "currentValue", currentValue);
        SetDecimal(root, "progressPercent", progressPercent);
        SetString(root, "frequency", frequency);
        SetString(root, "responsibleOffice", responsibleOffice);
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "{}";
    }

    private static JsonObject ParseObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        var node = JsonNode.Parse(json);
        return node as JsonObject ?? new JsonObject();
    }

    private static decimal? TryGetDecimal(JsonElement r, string name)
    {
        if (!r.TryGetProperty(name, out var p))
        {
            return null;
        }

        return p.ValueKind switch
        {
            JsonValueKind.Number => p.GetDecimal(),
            JsonValueKind.String => decimal.TryParse(p.GetString(), out var d) ? d : null,
            _ => null,
        };
    }

    private static string? TryGetString(JsonElement r, string name)
    {
        if (!r.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return p.GetString();
    }

    private static void SetDecimal(JsonObject root, string key, decimal? value)
    {
        if (value.HasValue)
        {
            root[key] = JsonValue.Create(value.Value);
        }
        else
        {
            _ = root.Remove(key);
        }
    }

    private static void SetString(JsonObject root, string key, string? value)
    {
        if (value is null)
        {
            _ = root.Remove(key);
            return;
        }

        var t = value.Trim();
        if (t.Length == 0)
        {
            _ = root.Remove(key);
        }
        else
        {
            root[key] = t;
        }
    }
}
