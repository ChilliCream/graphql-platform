using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Describes a binding to an object field.
/// </summary>
public readonly struct ObjectFieldBinding
{
    /// <summary>
    /// Creates a new instance of <see cref="ObjectFieldBinding"/>.
    /// </summary>
    /// <param name="name">
    /// The binding name.
    /// </param>
    /// <param name="type">
    /// The binding type.
    /// </param>
    /// <param name="replace">
    /// Defines if the bound property shall be replaced.
    /// </param>
    public ObjectFieldBinding(
        string name,
        ObjectFieldBindingType type,
        bool replace = true)
    {
        Name = name.EnsureGraphQLName();
        Type = type;
        Replace = replace;
    }

    /// <summary>
    /// Gets the binding name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the binding type.
    /// </summary>
    public ObjectFieldBindingType Type { get; }

    /// <summary>
    /// Defines if the bound property shall be replaced.
    /// </summary>
    /// <value></value>
    public bool Replace { get; }
}
