using HotChocolate.Types;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

/// <summary>
/// Represents an input object field.
/// </summary>
public class InputFieldModel : IFieldModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="InputFieldModel" />
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="description">The property xml documentation summary.</param>
    /// <param name="field"></param>
    /// <param name="type"></param>
    public InputFieldModel(
        string name,
        string? description,
        IInputField field,
        IInputType type)
    {
        Name = name.EnsureGraphQLName();
        Description = description;
        Field = field ?? throw new ArgumentNullException(nameof(field));
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// Gets the property name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the property xml documentation summary.
    /// </summary>
    public string? Description { get; }

    public IInputField Field { get; }

    IField IFieldModel.Field => Field;

    public IInputType Type { get; }

    IType IFieldModel.Type => Type;
}
