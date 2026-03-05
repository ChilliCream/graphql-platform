namespace ChilliCream.Nitro.CommandLine;

public interface IExtendedConsole : IAnsiConsole
{
    bool IsInteractive { get; set; }
}
