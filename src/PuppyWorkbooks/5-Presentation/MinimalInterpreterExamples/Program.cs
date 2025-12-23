
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

var engine = new RecalcEngine();
// notice the escaped quotes as we are passing a PowerFX string literal. Our second parameter needs to be 
//  a valid PowerFX expression.
engine.SetFormula("MyName", "\"Juanito\"", OnFormulaUpdate); 
engine.SetFormula("Greeting", "\"Hello, \" & MyName & \"!\"", OnFormulaUpdate);
var greetingResult = engine.Eval("Greeting");
Console.WriteLine(greetingResult.ToObject());
return;

void OnFormulaUpdate(string arg1, FormulaValue arg2)
{
    // this method is called whenever the formula is evaluated by the engine
}