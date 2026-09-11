using System.Text.Json;

namespace PuppyWorkbooks.Integration.Providers;

internal static class JsonPath
{
    public static IEnumerable<JsonElement> Select(JsonElement root, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "$") return [root];
        if (!path.StartsWith('$')) throw new InvalidOperationException("JsonPath must start with '$'.");
        var current = new List<JsonElement> { root };
        for (var index = 1; index < path.Length;)
        {
            if (path[index] == '.')
            {
                var start = ++index;
                while (index < path.Length && path[index] is not '.' and not '[') index++;
                var name = path[start..index];
                if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException($"Invalid JsonPath '{path}'.");
                current = current.Where(value => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out _))
                    .Select(value => value.GetProperty(name)).ToList();
                continue;
            }
            if (path[index] == '[')
            {
                var end = path.IndexOf(']', index);
                if (end < 0) throw new InvalidOperationException($"Invalid JsonPath '{path}'.");
                var selector = path[(index + 1)..end];
                current = selector == "*"
                    ? current.Where(value => value.ValueKind == JsonValueKind.Array).SelectMany(value => value.EnumerateArray()).ToList()
                    : SelectIndex(current, selector, path);
                index = end + 1;
                continue;
            }
            throw new InvalidOperationException($"Invalid JsonPath '{path}'.");
        }
        // The configured path identifies the input collection, so selecting an array
        // yields each object it contains without requiring a trailing [*].
        var selected = new List<JsonElement>();
        foreach (var value in current)
        {
            if (value.ValueKind == JsonValueKind.Array) selected.AddRange(value.EnumerateArray());
            else selected.Add(value);
        }
        return selected;
    }

    private static List<JsonElement> SelectIndex(List<JsonElement> values, string selector, string path)
    {
        if (!int.TryParse(selector, out var itemIndex) || itemIndex < 0) throw new InvalidOperationException($"Invalid JsonPath '{path}'.");
        return values.Where(value => value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > itemIndex)
            .Select(value => value[itemIndex]).ToList();
    }
}
