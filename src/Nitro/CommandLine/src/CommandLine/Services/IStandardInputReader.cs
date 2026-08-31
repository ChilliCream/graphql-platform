namespace ChilliCream.Nitro.CommandLine.Services;

/// <summary>
/// The process's standard input, injected so a hook adapter's payload can be
/// supplied without touching <see cref="Console.In"/>.
/// </summary>
internal interface IStandardInputReader
{
    TextReader Reader { get; }
}
