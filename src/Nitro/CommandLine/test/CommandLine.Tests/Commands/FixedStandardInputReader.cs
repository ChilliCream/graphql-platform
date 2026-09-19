using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands;

/// <summary>
/// Standard input backed by a fixed payload, in place of the process's own
/// console.
/// </summary>
internal sealed class FixedStandardInputReader(string payload) : IStandardInputReader
{
    public TextReader Reader { get; } = new StringReader(payload);
}
