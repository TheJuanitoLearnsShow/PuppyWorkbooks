using System.Collections;
using System.Text.Json;
using Microsoft.PowerFx.Types;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class MemoryInputProvider : IInputProvider
{
    private readonly object? _data;

    public MemoryInputProvider(object? data)
    {
        _data = data;
    }

    public MemoryInputProvider(string? jsonData)
    {
        _data = jsonData;
    }

    public async IAsyncEnumerable<IntegrationRecord> ReadAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        foreach (var recordValue in ToPowerFxRecords(_data))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in recordValue.Fields)
            {
                values[field.Name] = field.Value.ToObject();
            }
            yield return new IntegrationRecord(values);
        }
    }

    public static IEnumerable<RecordValue> ToPowerFxRecords(object? input)
    {
        if (input is null) yield break;

        if (input is RecordValue singleRecord)
        {
            yield return singleRecord;
            yield break;
        }

        if (input is TableValue tableValue)
        {
            foreach (var row in tableValue.Rows)
            {
                if (row.IsValue)
                    yield return row.Value;
            }
            yield break;
        }

        if (input is IEnumerable<RecordValue> recordList)
        {
            foreach (var r in recordList)
            {
                yield return r;
            }
            yield break;
        }

        if (input is string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString)) yield break;

            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(jsonString);
            }
            catch (JsonException)
            {
                // Not valid JSON string
            }

            if (doc is not null)
            {
                using (doc)
                {
                    foreach (var r in ConvertJsonElementToRecordValues(doc.RootElement))
                    {
                        yield return r;
                    }
                }
                yield break;
            }
        }

        if (input is JsonDocument jsonDoc)
        {
            foreach (var r in ConvertJsonElementToRecordValues(jsonDoc.RootElement))
            {
                yield return r;
            }
            yield break;
        }

        if (input is JsonElement jsonElement)
        {
            foreach (var r in ConvertJsonElementToRecordValues(jsonElement))
            {
                yield return r;
            }
            yield break;
        }

        if (input is IDictionary dict)
        {
            yield return ConvertDictionaryToRecordValue(dict);
            yield break;
        }

        if (input is IEnumerable enumerable and not string and not byte[])
        {
            foreach (var item in enumerable)
            {
                if (item is null) continue;
                if (item is RecordValue rv)
                {
                    yield return rv;
                }
                else if (item is IDictionary d)
                {
                    yield return ConvertDictionaryToRecordValue(d);
                }
                else
                {
                    using var doc = JsonSerializer.SerializeToDocument(item);
                    foreach (var r in ConvertJsonElementToRecordValues(doc.RootElement))
                    {
                        yield return r;
                    }
                }
            }
            yield break;
        }

        // Single POCO / anonymous type
        {
            using var doc = JsonSerializer.SerializeToDocument(input);
            foreach (var r in ConvertJsonElementToRecordValues(doc.RootElement))
            {
                yield return r;
            }
        }
    }

    private static IEnumerable<RecordValue> ConvertJsonElementToRecordValues(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                yield return ConvertJsonElementToRecordValue(item);
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            yield return ConvertJsonElementToRecordValue(element);
        }
        else
        {
            
            yield return FormulaValue.NewRecordFromFields(new NamedValue("Value", JsonElementToFormulaValue(element)));
        }
    }

    private static RecordValue ConvertJsonElementToRecordValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var fields = new List<NamedValue>();
            foreach (var property in element.EnumerateObject())
            {
                fields.Add(new NamedValue(property.Name, JsonElementToFormulaValue(property.Value)));
            }
            return FormulaValue.NewRecordFromFields(fields);
        }

        return FormulaValue.NewRecordFromFields(new NamedValue("Value", JsonElementToFormulaValue(element)));
    }

    private static FormulaValue JsonElementToFormulaValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => FormulaValue.New(element.GetString() ?? string.Empty),
            JsonValueKind.Number => element.TryGetInt64(out var l)
                ? FormulaValue.New(l)
                : (element.TryGetDecimal(out var dec) ? FormulaValue.New(dec) : FormulaValue.New(element.GetDouble())),
            JsonValueKind.True => FormulaValue.New(true),
            JsonValueKind.False => FormulaValue.New(false),
            JsonValueKind.Null or JsonValueKind.Undefined => FormulaValue.NewBlank(FormulaType.Blank),
            JsonValueKind.Object => ConvertJsonElementToRecordValue(element),
            _ => FormulaValue.New(element.ToString())
        };
    }

    private static RecordValue ConvertDictionaryToRecordValue(IDictionary dictionary)
    {
        var fields = new List<NamedValue>();
        foreach (DictionaryEntry entry in dictionary)
        {
            var key = entry.Key?.ToString() ?? string.Empty;
            fields.Add(new NamedValue(key, ObjectToFormulaValue(entry.Value)));
        }
        return FormulaValue.NewRecordFromFields(fields);
    }

    private static FormulaValue ObjectToFormulaValue(object? value)
    {
        if (value is null) return FormulaValue.NewBlank(FormulaType.Blank);
        if (value is FormulaValue fv) return fv;
        if (value is string s) return FormulaValue.New(s);
        if (value is bool b) return FormulaValue.New(b);
        if (value is int i) return FormulaValue.New(i);
        if (value is long l) return FormulaValue.New(l);
        if (value is double d) return FormulaValue.New(d);
        if (value is float f) return FormulaValue.New((double)f);
        if (value is decimal dec) return FormulaValue.New(dec);
        if (value is short sh) return FormulaValue.New((int)sh);
        if (value is byte by) return FormulaValue.New((int)by);
        if (value is DateTime dt) return FormulaValue.New(dt);
        if (value is Guid g) return FormulaValue.New(g.ToString());
        if (value is JsonElement je) return JsonElementToFormulaValue(je);
        if (value is JsonDocument jd) return JsonElementToFormulaValue(jd.RootElement);
        if (value is IDictionary dict) return ConvertDictionaryToRecordValue(dict);

        using var doc = JsonSerializer.SerializeToDocument(value);
        return JsonElementToFormulaValue(doc.RootElement);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
