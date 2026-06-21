namespace HotChocolate;

/// <summary>
/// Denotes a deprecated field on a GraphQL type, a deprecated value on a
/// GraphQL enum, or a deprecated GraphQL directive definition.
/// </summary>
[AttributeUsage(AttributeTargets.Class // Required for directive definitions
    | AttributeTargets.Field // Required for enum values
    | AttributeTargets.Property
    | AttributeTargets.Parameter
    | AttributeTargets.Method)]
public sealed class GraphQLDeprecatedAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLDeprecatedAttribute"/>
    /// with a specific deprecation reason.
    /// </summary>
    /// <param name="deprecationReason">The deprecation reason.</param>
    public GraphQLDeprecatedAttribute(string deprecationReason)
    {
        ArgumentException.ThrowIfNullOrEmpty(deprecationReason);

        DeprecationReason = deprecationReason;
    }

    /// <summary>
    /// The reason the field or enum value was deprecated.
    /// </summary>
    public string DeprecationReason { get; }
}
