namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Configuration settings for the GraphQL schema composition.
/// </summary>
public struct GraphQLCompositionSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether Global Object Identification should be enabled.
    /// </summary>
    public bool? EnableGlobalObjectIdentification { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether satisfiability paths should be included in the
    /// composed schema.
    /// </summary>
    public bool? IncludeSatisfiabilityPaths { get; set; }

    /// <summary>
    /// Gets or sets the set of tags whose annotated schema elements shall be excluded from
    /// composition.
    /// </summary>
    public ISet<string>? ExcludeByTag { get; set; }

    /// <summary>
    /// Gets or sets the environment name that shall be used for composition.
    /// </summary>
    public string? EnvironmentName { get; set; }
}
