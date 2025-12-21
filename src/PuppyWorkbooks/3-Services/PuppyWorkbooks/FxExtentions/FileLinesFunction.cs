using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace PuppyWorkbooks;

public class FileLinesFunction : ReflectionFunction
{
    public FileLinesFunction()
        : base("FileLines", FormulaType.UntypedObject, [ FormulaType.String ])
    {
    }

    public FormulaValue Execute(StringValue option)
    {
        var record = FormulaValue.NewRecordFromFields(
            new NamedValue("NewValue", option));
        return record;
    }
}