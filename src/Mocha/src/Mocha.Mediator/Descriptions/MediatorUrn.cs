namespace Mocha.Mediator;

/// <summary>
/// Builds the URN identity strings for mediator entities. URNs uniquely identify the mediator,
/// its handlers, and its messages within a service for use in descriptions, traces, and logs.
/// </summary>
internal static class MediatorUrn
{
    /// <summary>
    /// Returns a URN identifying the mediator within a service.
    /// </summary>
    public static string Mediator(string service)
        => $"urn:mocha:svc:{service}:mediator";

    /// <summary>
    /// Returns a URN identifying a handler within a service.
    /// </summary>
    public static string Handler(string service, string handler)
        => $"urn:mocha:svc:{service}:mediator:handler:{handler}";

    /// <summary>
    /// Returns a URN identifying a message (command, query, or notification) within a service.
    /// </summary>
    public static string Message(string service, string message)
        => $"urn:mocha:svc:{service}:mediator:message:{message}";
}
