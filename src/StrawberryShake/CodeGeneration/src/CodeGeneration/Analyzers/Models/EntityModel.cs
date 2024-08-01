using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

/// <summary>
/// Represents a entity that is used by a GraphQL client.
/// </summary>
public class EntityModel : ITypeModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="EntityModel" />.
    /// </summary>
    /// <param name="type">
    /// The entity type.
    /// </param>
    public EntityModel(IComplexOutputType type)
    {
        Name = type.Name;
        Type = type;
        Definition = type.GetEntityDefinition();

        var fields = new Dictionary<string, IOutputField>();

        foreach (var fieldSyntax in Definition.Selections.OfType<FieldNode>())
        {
            fields.Add(fieldSyntax.Name.Value, type.Fields[fieldSyntax.Name.Value]);
        }

        Fields = fields.Values.ToList();
    }

    /// <summary>
    /// Gets the type name of the entity.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the entity type.
    /// </summary>
    public INamedType Type { get; }

    /// <summary>
    /// Gets the entity definition that specifies the fields that make up the id fields.
    /// </summary>
    public SelectionSetNode Definition { get; }

    /// <summary>
    /// Gets the ID fields.
    /// </summary>
    public IReadOnlyList<IOutputField> Fields { get; }
}
