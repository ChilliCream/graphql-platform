namespace ChilliCream.Nitro.CommandLine.Services;

internal sealed class StandardInputReader : IStandardInputReader
{
    public TextReader Reader => Console.In;
}
