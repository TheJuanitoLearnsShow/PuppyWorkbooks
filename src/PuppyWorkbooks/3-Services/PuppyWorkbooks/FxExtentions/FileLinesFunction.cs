using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace PuppyWorkbooks;

public class FileLinesFunction : ReflectionFunction
{
    // neeed for the syntax checker to know the result type is a table with a column "Price" of type Decimal
    private static TableType _tableType = TableType.Empty().Add(new NamedFormulaType("Price", FormulaType.Decimal));
    public FileLinesFunction()
        : base("FileLines", _tableType, [ FormulaType.String ])
    {
    }

    public FormulaValue Execute(StringValue option)
    {
        // var record = FormulaValue.NewRecordFromFields(
        //     new NamedValue("NewValue", option));
        var table = new FileLinesTableValue(option.Value, _tableType);
        return table;
    }
}