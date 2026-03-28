using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsole : IAnsiConsole
{
    bool IsInteractive { get; }

    bool IsHumanReadable { get; }

    TextWriter Out { get; }

    TextWriter Error { get; }

    void SetOutputFormat(OutputFormat format);

    void WriteDirectly(string message);
}
