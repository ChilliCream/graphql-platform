namespace HotChocolate.Diagnostics;

public abstract class InstrumentationOptionsBase
{
    /// <summary>
    /// Specifies the request details that shall be included into the tracing activities.
    /// </summary>
    public RequestDetails RequestDetails { get; set; } = RequestDetails.Default;

    /// <summary>
    /// Specifies if the parsed document shall be included into the tracing data.
    /// </summary>
    public bool IncludeDocument { get; set; }

    internal bool IncludeRequestDetails => RequestDetails is not RequestDetails.None;
}
