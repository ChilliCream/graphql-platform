namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsole : IAnsiConsole
{
    bool IsInteractive { get; }

    void WriteErrorLine(string message);
}
