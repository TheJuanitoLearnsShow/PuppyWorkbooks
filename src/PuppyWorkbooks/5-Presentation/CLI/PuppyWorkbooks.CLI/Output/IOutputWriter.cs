using System.Text.Json;

namespace PuppyWorkbooks.CLI.Output;

public interface IOutputWriter : IDisposable
{
    void OpenWriter();
    void WriteCellResult(CellResult result);
    void WriteTestScenarioResult(WorkSheetTestScenarioResult result);
    void StartWorkbookResult(string workbookName);
    void EndWorkbookResult();
    void CloseWriter();
}

public class ConsoleOutputWriter : IOutputWriter, IAsyncDisposable
{
    private Utf8JsonWriter? _writer;
    private Stream _consoleStream;
    private bool _testScenariosStarted;

    public void OpenWriter()
    {
        _writer?.Dispose();
        _consoleStream = Console.OpenStandardOutput();
        _writer = new Utf8JsonWriter(_consoleStream, new JsonWriterOptions { Indented = true });
        
        // Write the beginning of a JSON object.
        _writer.WriteStartObject();
        _writer.WriteStartArray("results");
        return;
        
    }

    public void StartWorkbookResult(string workbookName)
    {
        if (_writer == null) return;
        _writer.WriteStartObject();
        _writer.WriteString("name", workbookName);
        _writer.WriteStartArray("cells");
        _testScenariosStarted = false;
    }

    public void EndWorkbookResult()
    {
        _writer?.WriteEndArray();
        _writer?.WriteEndObject();
        return;
    }
    public void WriteCellResult(CellResult result)
    {
        if (_writer == null) return ;
        _writer.WriteStartObject();
        _writer.WriteString("cellName", result.CellName);
        _writer.WriteNumber("cellId", result.CellId);
        _writer.WriteString("output", result.DisplayOutput);
        _writer.WriteEndObject();
        return;
    }

    public void WriteTestScenarioResult(WorkSheetTestScenarioResult result)
    {
        if (_writer == null) return;
        if (!_testScenariosStarted)
        {
            _writer.WriteEndArray();
            _writer.WriteStartArray("testScenarios");
            _testScenariosStarted = true;
        }

        _writer.WriteStartObject();
        _writer.WriteString("name", result.Name);
        _writer.WriteBoolean("passed", result.Passed);
        _writer.WriteStartArray("expectedCells");
        foreach (var cell in result.ExpectedCells)
        {
            _writer.WriteStartObject();
            _writer.WriteString("cellName", cell.CellName);
            _writer.WriteString("expectedValue", cell.ExpectedValue);
            _writer.WriteString("actualValue", cell.ActualValue);
            _writer.WriteBoolean("passed", cell.Passed);
            if (!string.IsNullOrWhiteSpace(cell.Error)) _writer.WriteString("error", cell.Error);
            _writer.WriteEndObject();
        }
        _writer.WriteEndArray();
        _writer.WriteEndObject();
    }

    public void CloseWriter()
    {
        if (_writer == null) return;
        _writer.WriteEndArray();
        _writer.WriteEndObject();
        _writer.Flush();
        _writer.Dispose();
        _writer = null;
        return;
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _consoleStream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_writer != null) await _writer.DisposeAsync();
        await _consoleStream.DisposeAsync();
    }
}
