using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// The <see cref="InputFieldConfiguration"/> contains the settings
/// to create a <see cref="InputField"/>.
/// </summary>
public class InputFieldConfiguration : ArgumentConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="InputFieldConfiguration"/>.
    /// </summary>
    public InputFieldConfiguration() { }

    /// <summary>
    /// Initializes a new instance of <see cref="InputFieldConfiguration"/>.
    /// </summary>
    public InputFieldConfiguration(
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

    internal void CopyTo(InputFieldConfiguration target)
    {
        base.CopyTo(target);

        target.Property = Property;
    }

    internal void MergeInto(InputFieldConfiguration target)
    {
        base.MergeInto(target);

        if (Property is not null)
        {
            target.Property = Property;
        }
    }
}
