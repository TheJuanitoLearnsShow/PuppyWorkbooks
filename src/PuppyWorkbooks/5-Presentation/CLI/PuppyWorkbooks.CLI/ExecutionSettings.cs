namespace PuppyWorkbooks.CLI;

public class ExecutionSettings
{
    /// <summary>
    /// Input path to JSON object containing a dictionary of formula values to inject
    /// into the workbooks. The value will be injected in each workbook that contains a
    /// formula with the name in key of the value.
    /// </summary>
    public string? InputDataPath { get; set; }
    
    public Dictionary<string, string> InputData { get; set; } = new();

    /// <summary>
    /// List of workbooks to execute. All the outputs of each workbook will be included in the
    /// results collection in the output.
    /// </summary>
    public string[] WorkbookPaths { get; set; } = [];

    public string? OutputPath { get; set; }
}