namespace HotChocolate.ApolloFederation;

/// <summary>
/// Represents the options of the Apollo Federation support.
/// </summary>
public sealed class ApolloFederationOptions
{
    /// <summary>
    /// Gets or sets the maximum number of representations that the <c>_entities</c> field
    /// resolves in a single request, counted across all of its selections and variable sets.
    /// </summary>
    /// <value>
    /// The maximum number of representations per request. Default is 10,000.
    /// </value>
    public int MaxAllowedRepresentations { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets the Apollo Federation version that the schema is linked to.
    /// </summary>
    /// <value>
    /// The Apollo Federation version. Default is <see cref="FederationVersion.Default"/>.
    /// </value>
    public FederationVersion Version { get; set; } = FederationVersion.Default;
}
