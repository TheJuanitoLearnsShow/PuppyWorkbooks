using System.Data.Common;

namespace PuppyWorkbooks.Integration.Engine;

public sealed class IntegrationRunnerOptions
{
    /// Creates a connection for SQL steps. Keeping this as a factory avoids coupling the
    /// integration library to one database vendor.
    public Func<string, DbConnection>? ConnectionFactory { get; init; }

    /// <summary>
    /// When true, captures debug execution details including input rows and cell results for each step.
    /// </summary>
    public bool Debug { get; set; }
}