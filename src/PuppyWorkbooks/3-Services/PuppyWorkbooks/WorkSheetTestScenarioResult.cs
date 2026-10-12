namespace PuppyWorkbooks;

public sealed class WorkSheetTestScenarioResult
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<WorkSheetExpectedCellResult> ExpectedCells { get; init; } = [];
    public bool Passed => ExpectedCells.All(cell => cell.Passed);
}

public sealed class WorkSheetExpectedCellResult
{
    public string CellName { get; init; } = string.Empty;
    public string ExpectedValue { get; init; } = string.Empty;
    public string? ActualValue { get; init; }
    public bool Passed { get; init; }
    public string? Error { get; init; }
}
