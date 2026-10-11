using System.Data.Common;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

internal static class SqlParameterBinder
{
    public static void AddParameters(DbCommand command, IEnumerable<SqlParameterOption> parameters,
        IntegrationRecord? record)
    {
        foreach (var option in parameters)
        {
            if (string.IsNullOrWhiteSpace(option.Name))
                throw new InvalidOperationException("A SQL parameter must specify a name.");

            var hasField = !string.IsNullOrWhiteSpace(option.FromField);
            var hasValue = option.FromValue is not null;
            if (hasField == hasValue)
                throw new InvalidOperationException(
                    $"SQL parameter '{option.Name}' must specify exactly one of FromField or FromValue.");

            object? value;
            if (hasField)
            {
                if (record is null)
                    throw new InvalidOperationException(
                        $"SQL parameter '{option.Name}' references input field '{option.FromField}', but no input record is available.");

                var field = record.Values.FirstOrDefault(pair =>
                    string.Equals(pair.Key, option.FromField, StringComparison.OrdinalIgnoreCase));
                if (field.Key is null)
                    throw new InvalidOperationException($"SQL parameter '{option.Name}' references missing input field '{option.FromField}'.");
                value = field.Value;
            }
            else
            {
                value = option.FromValue;
            }

            var parameter = command.CreateParameter();
            parameter.ParameterName = option.Name.StartsWith('@') ? option.Name : "@" + option.Name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
