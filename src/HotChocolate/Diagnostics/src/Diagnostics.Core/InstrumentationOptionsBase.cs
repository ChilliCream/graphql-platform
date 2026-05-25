namespace HotChocolate.Diagnostics;

public abstract class InstrumentationOptionsBase
{
    private int _maxErrorEvents = 10;

    /// <summary>
    /// Specifies the request details that shall be included into the tracing activities.
    /// </summary>
    public RequestDetails RequestDetails { get; set; } = RequestDetails.Default;

    /// <summary>
    /// Specifies if the parsed document shall be included into the tracing data.
    /// </summary>
    public bool IncludeDocument { get; set; }

    /// <summary>
    /// Specifies the maximum number of <c>graphql.error</c> events that will be
    /// emitted on the root <c>GraphQL Operation</c> span. The total error count
    /// remains available via the <c>graphql.error.count</c> tag, independent of
    /// this cap. The default is <c>10</c>, matching the OpenTelemetry GraphQL
    /// semantic conventions guidance. A value of <c>0</c> suppresses error events
    /// entirely.
    /// </summary>
    public int MaxErrorEvents
    {
        get => _maxErrorEvents;
        set => _maxErrorEvents = value < 0 ? 0 : value;
    }

    internal bool IncludeRequestDetails => RequestDetails is not RequestDetails.None;
}
