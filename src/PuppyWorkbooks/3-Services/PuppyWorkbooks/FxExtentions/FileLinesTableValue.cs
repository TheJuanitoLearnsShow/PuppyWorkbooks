using Microsoft.PowerFx.Types;

namespace PuppyWorkbooks;

public class FileLinesTableValue : TableValue
{
    private readonly string _path;
    private IEnumerable<DValue<RecordValue>> _rows;

    public FileLinesTableValue(string path)
        : base(TableType.Empty())
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
            var record = RecordValue.NewRecordFromFields(
                new NamedValue("Value", FormulaValue.New(line)));

            yield return DValue<RecordValue>.Of(record);
        }
    }

    public override IEnumerable<DValue<RecordValue>> Rows => GetRows();
}