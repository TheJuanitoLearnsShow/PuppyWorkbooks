using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace PuppyWorkbooks.WinUI.Reactor;


[Serializable]
public class FormulaEntry
{
    public string Name { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
}

[Serializable]
public class Workbook
{
    public string WorkbookName { get; set; } = "Untitled Workbook";
    public List<FormulaEntry> Entries { get; set; } = new();
}

public static class WorkbookStorage
{
    public static Workbook NewWorkbook() => new Workbook
    {
        WorkbookName = "Untitled Workbook",
        Entries = new List<FormulaEntry>()
    };

    public static Workbook LoadFromFile(string path)
    {
        using var fs = File.OpenRead(path);
        var serializer = new XmlSerializer(typeof(Workbook));
        return (Workbook)serializer.Deserialize(fs)!;
    }

    public static void SaveToFile(Workbook workbook, string path)
    {
        using var fs = File.Create(path);
        var serializer = new XmlSerializer(typeof(Workbook));
        serializer.Serialize(fs, workbook);
    }
}
