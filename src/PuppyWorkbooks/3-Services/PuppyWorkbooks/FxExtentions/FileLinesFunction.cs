using Microsoft.PowerFx.Types;

namespace PuppyWorkbooks;

public class FileLinesFunction : CustomFunction
{
    public FileLinesFunction()
        : base("FileLines", FormulaType.Table, new[] { FormulaType.String })
    {
    }

    public override Task<FormulaValue> InvokeAsync(
        FormulaValue[] args,
        CancellationToken cancellationToken)
    {
        if (args.Length != 1 || args[0] is not StringValue pathArg)
        {
            throw new ArgumentException("FileLines requires a single string argument (file path).");
        }

        var table = new FileLinesTableValue(IRContext.NotInSource(FormulaType.Table), pathArg.Value);
        return Task.FromResult<FormulaValue>(table);
    }
}