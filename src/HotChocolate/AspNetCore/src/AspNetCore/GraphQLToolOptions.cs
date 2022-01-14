using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Represents the GraphQL tool options for Banana Cake Pop.
/// </summary>
public class GraphQLToolOptions
{
    /// <summary>
    /// Gets or sets the website title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the default document content.
    /// </summary>
    public string? Document { get; set; }

    /// <summary>
    /// Defines that the schema endpoint URL shall be inferred from the browser URL.
    /// </summary>
    public bool UseBrowserUrlAsGraphQLEndpoint { get; set; } = true;

    /// <summary>
    /// Gets or sets the GraphQL endpoint.
    /// If <see cref="UseBrowserUrlAsGraphQLEndpoint"/> is set to <c>true</c> the
    /// GraphQL endpoint must be a relative path; otherwise, it must be an absolute URL.
    /// </summary>
    public string? GraphQLEndpoint { get; set; }

    /// <summary>
    /// Defines if cookies shall be included into the HTTP call to the GraphQL backend.
    /// </summary>
    public bool? IncludeCookies { get; set; }

    /// <summary>
    /// Gets or sets the default http headers for Banana Cake Pop.
    /// </summary>
    public IHeaderDictionary? HttpHeaders { get; set; }

    /// <summary>
    /// Gets or sets the default
    /// </summary>
    public DefaultHttpMethod? HttpMethod { get; set; }

    /// <summary>
    /// Defines if Banana Cake Pop is enabled.
    /// </summary>
    public bool Enable { get; set; } = true;

    /// <summary>
    /// Specifies the Google analytics tracking ID for Banana Cake Pop.
    /// </summary>
    public string? GaTrackingId { get; set; }

    /// <summary>
    /// Specifies if the application telemetry events are disabled.
    /// </summary>
    public bool? DisableTelemetry { get; set; }
}
