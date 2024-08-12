namespace HotChocolate;

/// <summary>
/// Denotes a deprecated field on a GraphQL type or a
/// deprecated value on a GraphQL enum.
/// </summary>
[AttributeUsage(AttributeTargets.Field // Required for enum values
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
        if (string.IsNullOrEmpty(deprecationReason))
        {
            throw new ArgumentNullException(nameof(deprecationReason));
        }

        DeprecationReason = deprecationReason;
    }

    /// <summary>
    /// The reason the field or enum value was deprecated.
    /// </summary>
    public string DeprecationReason { get; }
}
