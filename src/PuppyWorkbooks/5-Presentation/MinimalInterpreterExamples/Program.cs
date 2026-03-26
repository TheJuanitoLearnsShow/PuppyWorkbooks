
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

SimpleExample();
return;

void SimpleExample()
{
    var engine = new RecalcEngine();
// notice the escaped quotes as we are passing a PowerFX string literal. Our second parameter needs to be 
//  a valid PowerFX expression.
    engine.SetFormula("MyName", "\"Juanito\"", OnFormulaUpdate); 
    
    // engine.SetFormula("MyName", "\"Juanito\"", OnFormulaUpdate); // throws error because MyName is already defined (System.InvalidOperationException: MyName is already defined)
   
    engine.SetFormula("Greeting", "\"Hello, \" & MyName & \"!\"", OnFormulaUpdate);
    engine.SetFormula("EmailTxt", "\"jj@gamil.com\"", OnFormulaUpdate); 
    engine.SetFormula("IsEmail", 
                        """
                             If(
                                 IsMatch(
                                     EmailTxt,
                                    "^[0-9]$"
                                 ),
                                 true,
                                 false
                             )
                             """, OnFormulaUpdate); 
    var greetingResult = engine.Eval("Greeting");
    var isEmailResult = engine.Eval("IsEmail");
    Console.WriteLine(greetingResult.ToObject());
    Console.WriteLine(isEmailResult.ToObject());
    
    // 2nd Example
    engine.UpdateVariable("Velocity", 20);
    engine.SetFormula("Mass", "2", OnFormulaUpdate);
    engine.SetFormula("KineticEnergy", "(1/2) * Mass * Velocity * Velocity", OnFormulaUpdate);
    var energyResult = engine.GetValue("KineticEnergy");
    Console.WriteLine($"KineticEnergy: {greetingResult.ToObject()}");

    void OnFormulaUpdate(string arg1, FormulaValue arg2)
    {
        // this method is called whenever the formula is evaluated by the engine
    }
}