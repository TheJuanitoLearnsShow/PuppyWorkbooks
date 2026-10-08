using System.Text.Json;

namespace PuppyWorkbooks.CLI;

class InputDataProvider
{
    
    public static Dictionary<string, string> LoadInputValues(ExecutionSettings settings)
    {
        var inputValues = new Dictionary<string, string>(settings.InputData, StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(settings.InputDataPath) || !File.Exists(settings.InputDataPath) )
        {
            return inputValues;
        }

        var jsonContent = File.ReadAllText(settings.InputDataPath);

        try
        {
            var valuesFromInputFile = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
            if (valuesFromInputFile is not null)
            {
                foreach (var kv in valuesFromInputFile)
                {
                    if (!inputValues.ContainsKey(kv.Key))
                    {
                        inputValues[kv.Key] = kv.Value;
                    }
                }
            }
        }
        catch (JsonException)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (!inputValues.ContainsKey(prop.Name))
                        {
                            inputValues[prop.Name] = prop.Value.ToString();
                        }
                    }
                }
            }
            catch (JsonException) { }
        }

        return inputValues;
    }
}