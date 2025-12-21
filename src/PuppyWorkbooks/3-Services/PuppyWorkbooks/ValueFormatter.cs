using Microsoft.PowerFx.Types;

namespace PuppyWorkbooks;

public class ValueFormatter
{
    public static string ToDisplayOutput(FormulaValue result)
    {
        return result.Type switch
        {
            TableType tableType => DisplayTable(result as TableValue),
            // Runtime tableType => DisplayTable(result as TableValue),
            RecordType recordType => DisplayRecord(result as RecordValue),
            _ => result?.ToObject().ToString() ?? string.Empty
        };
    }

    private static string DisplayRecord(RecordValue? result)
    {
        if (result == null) return string.Empty;
        var fields = string.Join(", ", result.Fields.Select(f => $"{f.Name}: {ToDisplayOutput(f.Value)}"));
        return fields;
    }

    private static string DisplayTable(TableValue table)
    {
        IList<string> colNames = [string.Join(", ", table.Type.FieldNames)];
        var rows = table.Rows.Select(r => ToDisplayOutput(r.Value));// string.Join(", ", r.Value)).ToList();
        return string.Join(Environment.NewLine, colNames.Concat(rows));
    }
}