using HotChocolate.Types;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

public class LeafTypeModel : ITypeModel
{
    public LeafTypeModel(
        string name,
        string? description,
        ILeafType type,
        string serializationType,
        string runtimeType)
    {
        Name = name.EnsureGraphQLName();
        Description = description;
        Type = type ??
               throw new ArgumentNullException(nameof(type));
        SerializationType = serializationType ??
                            throw new ArgumentNullException(nameof(serializationType));
        RuntimeType = runtimeType ??
                      throw new ArgumentNullException(nameof(runtimeType));
    }

    /// <summary>
    /// Gets the enum name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the enum xml documentation summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the leaf type.
    /// </summary>
    public ILeafType Type { get; }

    INamedType ITypeModel.Type => Type;

    public string SerializationType { get; }

    public string RuntimeType { get; }
}
