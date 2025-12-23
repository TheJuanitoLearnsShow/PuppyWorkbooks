using Microsoft.PowerFx.Types;

namespace PuppyWorkbooks;

public class FileLinesTableValue : TableValue
{
    private readonly string _path;
    private IEnumerable<DValue<RecordValue>> _rows;

    public FileLinesTableValue(string path, TableType tableType)
        : base(tableType) // potentially pass the column defnition from the function
    {
        _path = path;
    }

    // This is the key: override GetRows() to lazily yield records
    public IEnumerable<DValue<RecordValue>> GetRows()
    {
        using var reader = new StreamReader(_path);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();

            // Wrap each line into a RecordValue with a single column "Value"
            // var record = RecordValue.NewRecordFromFields(
            //     new NamedValue("Value", FormulaValue.New(line)));

            if (decimal.TryParse(line, out var numberValue))
            {
                
                var recordDecimal = RecordValue.NewRecordFromFields(
                    new NamedValue("Price", FormulaValue.New(numberValue)));

                yield return DValue<RecordValue>.Of(recordDecimal);
            }
            else
            {
                var record = RecordValue.NewRecordFromFields(
                    new NamedValue("Value", FormulaValue.New(line)));

                yield return DValue<RecordValue>.Of(record);
            }
        }
    }

    public override IEnumerable<DValue<RecordValue>> Rows => GetRows();
}