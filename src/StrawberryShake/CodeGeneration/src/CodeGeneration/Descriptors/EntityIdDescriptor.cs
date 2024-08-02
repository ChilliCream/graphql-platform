namespace StrawberryShake.CodeGeneration.Descriptors;

/// <summary>
/// Represents the entity for which the ID shall be generated or an id field of that entity.
/// </summary>
public sealed class EntityIdDescriptor
{
    /// <summary>
    /// Creates a new instance of <see cref="EntityIdDescriptor"/>.
    /// </summary>
    /// <param name="name">
    /// The GraphQL name of entity entity.
    /// </param>
    /// <param name="typeName">
    /// The serialization type name of the entity id field, eg. String.
    /// </param>
    /// <param name="fields">
    /// The child fields.
    /// </param>
    public EntityIdDescriptor(
        string name,
        string typeName,
        IReadOnlyList<ScalarEntityIdDescriptor> fields )
    {
        Name = name;
        TypeName = typeName;
        Fields = fields;
    }

    /// <summary>
    /// Gets the name of the field or entity.
    /// </summary>
    /// <value></value>
    public string Name { get; }

    /// <summary>
    /// Gets the type name of the entity id field.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Gets the child fields.
    /// </summary>
    public IReadOnlyList<ScalarEntityIdDescriptor> Fields { get; }
}
