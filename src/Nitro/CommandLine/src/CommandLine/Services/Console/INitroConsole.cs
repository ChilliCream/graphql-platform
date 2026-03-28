namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsole : IAnsiConsole
{
    bool IsInteractive { get; }

    TextWriter Out { get; }

    TextWriter Error { get; }
}
