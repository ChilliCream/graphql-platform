namespace ChilliCream.Nitro.CommandLine.Output;

/// <summary>
/// The schema version of the <see cref="OutputEnvelope{T}"/> JSON shape. Bump this when the
/// envelope contract changes in a way that would break existing consumers.
/// </summary>
internal static class OutputEnvelopeVersion
{
    public const int Current = 1;
}

/// <summary>
/// The time window that the analytical command was executed against.
/// </summary>
internal sealed record OutputEnvelopeWindow(DateTimeOffset From, DateTimeOffset To);

/// <summary>
/// The error payload returned in the envelope when a command fails. Mirrors the shape of
/// the success payload so consumers can distinguish via the presence of <c>data</c> versus
/// <c>error</c>.
/// </summary>
internal sealed record OutputEnvelopeError(string Code, string Message);

/// <summary>
/// The versioned envelope that wraps every payload produced by an analytical command.
/// Either <see cref="Data"/> or <see cref="Error"/> is non-null; never both.
/// </summary>
/// <typeparam name="T">The shape of the success payload.</typeparam>
internal sealed record OutputEnvelope<T>(
    int Version,
    string Api,
    string Stage,
    OutputEnvelopeWindow Window,
    T? Data,
    OutputEnvelopeError? Error)
{
    public static OutputEnvelope<T> Success(
        string api,
        string stage,
        OutputEnvelopeWindow window,
        T data)
        => new(OutputEnvelopeVersion.Current, api, stage, window, data, null);

    public static OutputEnvelope<T> Failure(
        string api,
        string stage,
        OutputEnvelopeWindow window,
        OutputEnvelopeError error)
        => new(OutputEnvelopeVersion.Current, api, stage, window, default, error);
}
