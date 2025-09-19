namespace ChilliCream.Nitro.CommandLine;

public interface IExtendedConsole : IAnsiConsole
{
    public bool IsInteractive { get; set; }
}
