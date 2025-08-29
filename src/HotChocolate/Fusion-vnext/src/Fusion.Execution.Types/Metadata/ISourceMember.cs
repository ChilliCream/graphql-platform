namespace HotChocolate.Fusion.Types.Metadata;

/// <summary>
/// Represents metadata about how a composite type system member appears in a specific source schema.
/// </summary>
/// <remarks>
/// In a federated GraphQL setup, a single composite type system member (type, field, directive, etc.)
/// may be present in multiple source schemas, potentially with different characteristics in each.
/// The source member metadata capture the  source-specific properties and metadata for how the composite
/// member is defined in one particular source schema, enabling proper query planning and execution.
/// </remarks>
public interface ISourceMember
{
    /// <summary>
    /// Gets the name of the composite type system member as it appears in this specific source schema.
    /// </summary>
    /// <value>
    /// The member name as defined in this source schema. This represents how the composite
    /// member is named within the context of this particular source schema.
    /// </value>
    string Name { get; }

    /// <summary>
    /// Gets the name of the source schema that contains this type system member.
    /// </summary>
    /// <value>
    /// The unique identifier of the source schema within the composite schema setup.
    /// This corresponds to the schema names defined in the composite schema configuration.
    /// </value>
    string SchemaName { get; }
}
