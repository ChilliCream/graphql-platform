using System.Text;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the metadata associated with an entity.
/// </summary>
internal sealed class EntityMetadata
{
    /// <summary>
    /// Gets the list of entity resolvers associated with this entity.
    /// </summary>
    public EntityResolversCollection EntityResolvers { get; } = [];

    /// <summary>
    /// Gets fields that have dependencies on other fields.
    /// </summary>
    public Dictionary<string, List<FieldDependency>> DependantFields { get; } = new();

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();

        foreach (var resolver in EntityResolvers)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine(resolver.ToString());
        }

        return sb.ToString();
    }
}
