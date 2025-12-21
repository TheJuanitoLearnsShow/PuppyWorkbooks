using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace PuppyWorkbooks;

public class FileLinesFunction : ReflectionFunction
{
    public FileLinesFunction()
        : base("FileLines", TableType.Empty(), [ FormulaType.String ])
    {
    }

    public FormulaValue Execute(StringValue option)
    {
        // var record = FormulaValue.NewRecordFromFields(
        //     new NamedValue("NewValue", option));
        var table = new FileLinesTableValue(option.Value);
        return table;
    }
}