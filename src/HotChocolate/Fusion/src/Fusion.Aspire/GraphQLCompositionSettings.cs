using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Configuration settings for the GraphQL schema composition.
/// </summary>
public struct GraphQLCompositionSettings
{
    /// <summary>
    /// Gets or sets how <c>@cacheControl</c> directives are merged.
    /// </summary>
    public DirectiveMergeBehavior? CacheControlMergeBehavior { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Global Object Identification should be enabled.
    /// </summary>
    public bool? EnableGlobalObjectIdentification { get; set; }

    /// <summary>
    /// Gets or sets how the gateway resolves the <c>Query.node</c> field.
    /// </summary>
    public NodeResolution? NodeResolution { get; set; }

    /// <summary>
    /// Gets or sets how <c>@tag</c> directives are merged.
    /// </summary>
    public DirectiveMergeBehavior? TagMergeBehavior { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether satisfiability paths should be included in the
    /// composed schema.
    /// </summary>
    public bool? IncludeSatisfiabilityPaths { get; set; }

    /// <summary>
    /// Gets or sets whether Apollo Federation non-resolvable interface objects are accepted and
    /// unresolved projected fields are reported as field errors at runtime.
    /// </summary>
    public bool? AllowNonResolvableInterfaceObjects { get; set; }

    /// <summary>
    /// Gets or sets how runtime types are routed for Apollo Federation shareable fields whose
    /// result type is abstract.
    /// </summary>
    public ShareableFieldRuntimeTypeRouting? ShareableFieldRuntimeTypeRouting { get; set; }

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
