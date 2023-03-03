namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents a group of related entity parts that together make up a complete entity.
/// </summary>
internal sealed record EntityGroup
{
    /// <summary>
    /// Creates a new instance of the <see cref="EntityGroup"/> class.
    /// </summary>
    /// <param name="name">The name of the entity group.</param>
    /// <param name="parts">The list of entity parts that make up the entity group.</param>
    public EntityGroup(
        string name,
        IReadOnlyList<EntityPart> parts)
    {
        Name = name;
        Parts = parts;
    }

    /// <summary>
    /// Gets the name of the entity group.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the list of entity parts that make up the entity group.
    /// </summary>
    public IReadOnlyList<EntityPart> Parts { get; }

    /// <summary>
    /// Gets the metadata associated with this entity group.
    /// </summary>
    public EntityMetadata Metadata { get; } = new();

    /// <summary>
    /// Deconstructs the entity group into its name and parts.
    /// </summary>
    /// <param name="name">
    /// The name of the entity group.
    /// </param>
    /// <param name="parts">
    /// The list of entity parts that make up the entity group.
    /// </param>
    /// <param name="metadata">
    /// The metadata associated with this entity group.
    /// </param>
    public void Deconstruct(
        out string name,
        out IReadOnlyList<EntityPart> parts,
        out EntityMetadata metadata)
    {
        name = Name;
        parts = Parts;
        metadata = Metadata;
    }
}
