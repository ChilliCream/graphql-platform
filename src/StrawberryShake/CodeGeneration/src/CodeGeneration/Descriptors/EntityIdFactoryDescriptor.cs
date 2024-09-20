namespace StrawberryShake.CodeGeneration.Descriptors;

/// <summary>
/// Represents the code descriptor of the entity id factor.
/// </summary>
public sealed class EntityIdFactoryDescriptor : ICodeDescriptor
{
    /// <summary>
    /// Creates a new instance of <see cref="EntityIdFactoryDescriptor"/>.
    /// </summary>
    /// <param name="name">
    /// The class name of the entity id factory.
    /// </param>
    /// <param name="entities">
    /// The entity descriptors.
    /// </param>
    /// <param name="namespace">
    /// The namespace of this class.
    /// </param>
    public EntityIdFactoryDescriptor(
        string name,
        IReadOnlyList<EntityIdDescriptor> entities,
        string @namespace)
    {
        Name = name;
        Entities = entities;
        Namespace = @namespace;
        Type = new RuntimeTypeInfo(name, @namespace);
    }

    /// <summary>
    /// Gets the class name of the entity id factory.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the namespace of this factory class.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// The type of the factory
    /// </summary>
    public RuntimeTypeInfo Type { get; }

    /// <summary>
    /// Gets the entity descriptors.
    /// </summary>
    public IReadOnlyList<EntityIdDescriptor> Entities { get; }
}
