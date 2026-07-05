using PuppyWorkbooks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace PuppyWorkSheets.WinUI.Reactor;


public static class WorkSheetStorage
{
    public static WorkSheet NewWorkSheet() => new WorkSheet
    {
        Name = "Untitled WorkSheet",
        Cells = new List<WorkCell>()
    };

    public static WorkSheet LoadFromFile(string path)
    {
        using var fs = File.OpenRead(path);
        var serializer = new XmlSerializer(typeof(WorkSheet));
        return (WorkSheet)serializer.Deserialize(fs)!;
    }

    public static void SaveToFile(WorkSheet worksheet, string path)
    {
        using var fs = File.Create(path);
        var serializer = new XmlSerializer(typeof(WorkSheet));
        serializer.Serialize(fs, worksheet);
    }
}
