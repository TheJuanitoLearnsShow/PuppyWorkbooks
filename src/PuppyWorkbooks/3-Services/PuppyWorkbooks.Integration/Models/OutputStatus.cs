namespace PuppyWorkbooks.Integration.Models;

public sealed record OutputStatus(bool Succeeded, string Message, int AffectedRows = 1)
{
    public static OutputStatus Success(string message, int rows = 1) => new(true, message, rows);
}