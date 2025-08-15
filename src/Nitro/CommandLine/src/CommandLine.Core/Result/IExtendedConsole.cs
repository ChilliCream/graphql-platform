namespace ChilliCream.Nitro;

public interface IExtendedConsole : IAnsiConsole
{
    public bool IsInteractive { get; set; }
}
