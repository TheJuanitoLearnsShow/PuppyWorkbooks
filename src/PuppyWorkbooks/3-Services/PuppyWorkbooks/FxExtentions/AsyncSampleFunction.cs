using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace PuppyWorkbooks;

public class AsyncSampleFunction : ReflectionFunction
{
    public AsyncSampleFunction()
        : base("AsyncSample", FormulaType.UntypedObject, [ FormulaType.String ])
    {
    }

    /// <summary>
    /// cancellationToken is required because the PowerFX interpreter needs to pass it.
    /// </summary>
    /// <param name="option"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<FormulaValue> Execute(StringValue option, CancellationToken cancellationToken)
    {
        await Task.Delay(1000);
        var record = FormulaValue.NewRecordFromFields(
            new NamedValue("AsyncNewValue", option));
        return record;
    }
}