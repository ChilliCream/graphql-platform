namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal sealed class FilterParseException(string message, int column) : Exception(message)
{
    public int Column { get; } = column;
}
