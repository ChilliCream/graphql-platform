namespace ChilliCream.Nitro.CommandLine.Output;

/// <summary>
/// The explicit error codes emitted by analytical commands in the failure envelope.
/// Consumers can switch on <see cref="OutputEnvelopeError.Code"/> to react to a known
/// failure mode instead of parsing the human-readable message.
/// </summary>
internal static class ErrorCodes
{
    public const string CoordinateNotFound = "COORDINATE_NOT_FOUND";
    public const string StageNotFound = "STAGE_NOT_FOUND";
    public const string ApiNotFound = "API_NOT_FOUND";
    public const string NoDataInWindow = "NO_DATA_IN_WINDOW";
    public const string NotAuthenticated = "NOT_AUTHENTICATED";
    public const string InvalidTimeWindow = "INVALID_TIME_WINDOW";
}
