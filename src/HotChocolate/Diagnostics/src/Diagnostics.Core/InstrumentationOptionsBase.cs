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

    /// <summary>
    /// Specifies whether the root GraphQL request span is given a more descriptive
    /// display name. When enabled, the span is named using the
    /// <c>{graphql.operation.type} {graphql.operation.name}</c> format when the
    /// operation name is available and the operation is successfully identified in
    /// the document. The default is
    /// <c>false</c>. Only enable this for operation domains with bounded
    /// cardinality (e.g. persisted operations) to avoid high-cardinality span names.
    /// </summary>
    public bool IncludeOperationNameInSpanName { get; set; }

    internal bool IncludeRequestDetails => RequestDetails is not RequestDetails.None;
}
