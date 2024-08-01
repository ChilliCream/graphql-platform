using HotChocolate.Types;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

/// <summary>
/// Represents an input object type model.
/// </summary>
public sealed class InputObjectTypeModel : ITypeModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="InputObjectTypeModel" />
    /// </summary>
    /// <param name="name">The class name.</param>
    /// <param name="description">The class description.</param>
    /// <param name="type">The input object type.</param>
    /// <param name="fields">The field models of this input type.</param>
    /// <param name="hasUpload">
    /// Defines if this input or one of its related has a upload scalar
    /// </param>
    public InputObjectTypeModel(
        string name,
        string? description,
        InputObjectType type,
        bool hasUpload,
        IReadOnlyList<InputFieldModel> fields)
    {
        Name = name.EnsureGraphQLName();
        Description = description;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        HasUpload = hasUpload;
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }

    /// <summary>
    /// Gets the class name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the class xml documentation summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the input object type.
    /// </summary>
    public InputObjectType Type { get; }

    /// <summary>
    /// Defines if this input or one of its related has a upload scalar
    /// </summary>
    public bool HasUpload { get; }

    INamedType ITypeModel.Type => Type;

    /// <summary>
    /// Gets the field models of this input type.
    /// </summary>
    public IReadOnlyList<InputFieldModel> Fields { get; }
}
