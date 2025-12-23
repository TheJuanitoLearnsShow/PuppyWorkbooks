using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace PuppyWorkbooks;

public class AddTaxFunction : ReflectionFunction
{
    public AddTaxFunction()
        : base("AddTax", FormulaType.Decimal ,[ FormulaType.Decimal ])
    {
    }

    public FormulaValue Execute(DecimalValue input)
    {
        var result = input.Value * 1.07m; // Add 7% tax
        return FormulaValue.New(result);
    }
}