using HotChocolate.Execution.Serialization;

namespace HotChocolate.AspNetCore.Serialization;

/// <summary>
/// Represents the GraphQL over HTTP formatter options.
/// </summary>
public struct HttpResponseFormatterOptions
{
    /// <summary>
    /// Gets or sets the GraphQL over HTTP transport version.
    /// </summary>
    public HttpTransportVersion HttpTransportVersion { get; set; }

    /// <summary>
    /// Gets or sets the JSON result formatter options.
    /// </summary>
    public JsonResultFormatterOptions Json { get; set; }
}
