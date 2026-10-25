using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands;

/// <summary>
/// Provides a shared text reader over <paramref name="payload"/>.
/// </summary>
internal sealed class FixedStandardInputReader(string payload) : IStandardInputReader
{
    public TextReader Reader { get; } = new StringReader(payload);
}
