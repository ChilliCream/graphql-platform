namespace ChilliCream.Nitro.CommandLine;

internal static class CommandExecutionContext
{
    private static readonly AsyncLocal<ICommandServices> s_services = new();

    internal static ICommandServices Services => s_services.Value
        ?? throw new InvalidOperationException("Command services have not been initialized.");

    internal static ICommandServices? TryGetServices() => s_services.Value;

    internal static void Initialize(ICommandServices services) => s_services.Value = services;
}
