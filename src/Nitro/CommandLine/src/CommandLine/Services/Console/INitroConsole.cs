using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsole : IAnsiConsole
{
    bool IsInteractive { get; }

    bool IsHumanReadable { get; }

    bool HasWrittenOutput { get; }

    IAnsiConsole Out { get; }

    IAnsiConsole Error { get; }

    void SetOutputFormat(OutputFormat format);
}
