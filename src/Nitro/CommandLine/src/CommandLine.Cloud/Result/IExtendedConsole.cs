namespace ChilliCream.Nitro.CLI.Results;

internal interface IExtendedConsole : IAnsiConsole
{
    public bool IsInteractive { get; set; }
}
