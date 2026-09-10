namespace Mocha.TestHelpers;

/// <summary>
/// The headers a faulted message carries, named by the constants the fault middleware writes.
/// </summary>
public static class FaultHeaders
{
    public static string ExceptionType => MessageHeaders.Fault.ExceptionType.Key;

    public static string Message => MessageHeaders.Fault.Message.Key;

    public static string StackTrace => MessageHeaders.Fault.StackTrace.Key;

    public static string Timestamp => MessageHeaders.Fault.Timestamp.Key;

    private static string[] Keys => [ExceptionType, Message, StackTrace, Timestamp];

    /// <summary>
    /// The CLR type each fault header value must have.
    /// </summary>
    public static Dictionary<string, string> ExpectedShape()
        => Keys.ToDictionary(key => key, _ => nameof(String));

    /// <summary>
    /// Maps the fault headers of a received message onto each value's CLR type, naming any absent.
    /// </summary>
    public static Dictionary<string, string> Shape(IReadOnlyDictionary<string, object?> headers)
        => Keys.ToDictionary(
            key => key,
            key => headers.TryGetValue(key, out var value)
                ? value?.GetType().Name ?? "<null>"
                : "<missing>");
}
