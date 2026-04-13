namespace ChilliCream.Nitro.CommandLine.Output;

/// <summary>
/// Renders an analytical command payload to a single output format. Implementations are
/// per-command and per-format and do not share state.
/// </summary>
/// <typeparam name="T">The shape of the success payload.</typeparam>
internal interface IOutputFormatter<T>
{
    /// <summary>
    /// Writes the success envelope to the supplied console.
    /// </summary>
    void Write(INitroConsole console, OutputEnvelope<T> envelope);

    /// <summary>
    /// Writes the failure envelope to the supplied console.
    /// </summary>
    void WriteError(INitroConsole console, OutputEnvelope<T> envelope);
}
