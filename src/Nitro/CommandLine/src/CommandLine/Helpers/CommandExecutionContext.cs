namespace ChilliCream.Nitro.CommandLine;

internal static class CommandExecutionContext
{
    internal static readonly AsyncLocal<ICommandServices> s_services = new();
}
