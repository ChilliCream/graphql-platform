using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Descriptors;

public sealed class EntityTypeDescriptor : ICodeDescriptor
{
    /// <summary>
    /// Create a new instance of <see cref="EntityTypeDescriptor" />
    /// </summary>
    /// <param name="name">
    /// The name of the GraphQL type
    /// </param>
    /// <param name="runtimeType"></param>
    /// <param name="properties">
    /// The properties of this entity.
    /// </param>
    /// <param name="documentation">
    /// The documentation of this entity
    /// </param>
    public EntityTypeDescriptor(
        string name,
        RuntimeTypeInfo runtimeType,
        Dictionary<string, PropertyDescriptor> properties,
        string? documentation)
    {
        Name = name;
        RuntimeType = runtimeType;
        Properties = properties;
        Documentation = documentation;
    }

    /// <summary>
    /// Gets the GraphQL type name which this entity represents.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the entity name.
    /// </summary>
    public RuntimeTypeInfo RuntimeType { get; }

    /// <summary>
    /// The documentation of this type
    /// </summary>
    public string? Documentation { get; }

    /// <summary>
    /// Gets the properties of this entity.
    /// </summary>
    public Dictionary<string, PropertyDescriptor> Properties { get; }
}
