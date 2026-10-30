using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Engine;

public sealed class ValueBinder
{
    private static readonly Regex InputTemplatePattern = new(
        @"\{\{\s*InputRecord\.([\w.]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool ToBoolean(object? value) => value is bool b
        ? b
        : bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;

    public static IntegrationRecord CreateStateRecord(string outputField, object? state)
    {
        if (state is IDictionary dictionary)
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry item in dictionary)
                values[Convert.ToString(item.Key, CultureInfo.InvariantCulture)!] = item.Value;
            return new IntegrationRecord(values);
        }

        if (state is JsonElement { ValueKind: JsonValueKind.Object } jsonObject)
        {
            var values = jsonObject.Deserialize<Dictionary<string, object?>>() ?? [];
            return new IntegrationRecord(values);
        }

        return new IntegrationRecord(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [outputField] = state
        });
    }

    public static object? ParseInitialState(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    public static void BindRecord(WorkSheet worksheet, IntegrationRecord record)
    {
        // InputRecord is the stable worksheet contract. Direct field bindings remain
        // available for compatibility with existing worksheets.
        BindValue(worksheet, "InputRecord", record.Values);
        foreach (var pair in record.Values) BindValue(worksheet, pair.Key, pair.Value);
    }

    public static void BindValue(WorkSheet worksheet, string name, object? value)
    {
        if (worksheet.Variables.ContainsKey(name))
        {
            worksheet.Variables[name] = ToFormulaLiteral(value);
            return;
        }

        var cell = worksheet.Cells.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        if (cell is not null) cell.Formula = ToFormulaLiteral(value);
        else worksheet.Cells.Insert(0, new WorkCell(0, name, ToFormulaLiteral(value), "integration input"));
    }

    private static string ToFormulaLiteral(object? value)
    {
        if (value is null) return "Blank()";
        if (value is JsonElement json)
        {
            return json.ValueKind switch
            {
                JsonValueKind.String => JsonSerializer.Serialize(json.GetString()),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "Blank()",
                JsonValueKind.Object => ToFormulaLiteral(json.Deserialize<Dictionary<string, object?>>() ?? []),
                JsonValueKind.Array => "[" + string.Join(",", json.EnumerateArray().Select(item => ToFormulaLiteral(item))) + "]",
                _ => json.ToString()
            };
        }
        if (value is bool b) return b ? "true" : "false";
        if (value is string s) return JsonSerializer.Serialize(s);
        if (value is IDictionary dictionary) return "{" + string.Join(",", dictionary.Keys.Cast<object>().Select(k => $"{k}: {ToFormulaLiteral(dictionary[k])}")) + "}";
        if (value is IEnumerable enumerable and not byte[])
            return "[" + string.Join(",", enumerable.Cast<object?>().Select(ToFormulaLiteral)) + "]";
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "Blank()";
    }
    
    public static string FormatInputValue(object? value) => value switch
    {
        null => string.Empty,
        JsonElement { ValueKind: JsonValueKind.String } json => json.GetString() ?? string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };
    
    public static string ResolveFieldOrValue(string staticValue, string fromField, 
        IntegrationRecord? inputRecord, SecretManager? secretManager)
    {
        if (!string.IsNullOrWhiteSpace(fromField))
        {
            if (inputRecord is not null)
            {
                var field = inputRecord.Values.FirstOrDefault(pair => string.Equals(pair.Key, fromField, StringComparison.OrdinalIgnoreCase));
                if (field.Key is not null)
                {
                    return ValueBinder.FormatInputValue(field.Value);
                }
                throw new InvalidOperationException($"Step references missing input field '{fromField}'.");
            }
        }

        return ResolveValue(staticValue, inputRecord, secretManager);
    }

    public static string ResolveValue(string value, IntegrationRecord? inputRecord = null, SecretManager? secretManager = null)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var resolved = secretManager?.Resolve(value) ?? value;
        if (inputRecord is null) return resolved;
        
        return InputTemplatePattern.Replace(resolved, match =>
        {
            var fieldName = match.Groups[1].Value;
            var field = inputRecord.Values.FirstOrDefault(pair => string.Equals(pair.Key, fieldName, StringComparison.OrdinalIgnoreCase));
            if (field.Key is null)
                throw new InvalidOperationException($"Input step references missing input field '{fieldName}'.");
            return ValueBinder.FormatInputValue(field.Value);
        });
    }
}