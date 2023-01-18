using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// The <see cref="InputFieldDefinition"/> contains the settings
/// to create a <see cref="InputField"/>.
/// </summary>
public class InputFieldDefinition : ArgumentDefinition
{
    /// <summary>
    /// Initializes a new instance of <see cref="InputFieldDefinition"/>.
    /// </summary>
    public InputFieldDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="InputFieldDefinition"/>.
    /// </summary>
    public InputFieldDefinition(
        string name,
        string? description = null,
        TypeReference? type = null,
        IValueNode? defaultValue = null,
        object? runtimeDefaultValue = null)
        : base(name, description, type, defaultValue, runtimeDefaultValue)
    {
    }

    /// <summary>
    /// Gets the associated property.
    /// </summary>
    public PropertyInfo? Property { get; set; }

    internal void CopyTo(InputFieldDefinition target)
    {
        base.CopyTo(target);

        target.Property = Property;
    }

    internal void MergeInto(InputFieldDefinition target)
    {
        base.MergeInto(target);

        if (Property is not null)
        {
            target.Property = Property;
        }
    }
}
