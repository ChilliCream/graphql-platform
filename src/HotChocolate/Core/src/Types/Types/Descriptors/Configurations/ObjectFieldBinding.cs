using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors.Configurations;

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
    /// Creates a new instance of <see cref="ObjectFieldBinding"/>
    /// from a runtime member.
    /// </summary>
    /// <param name="member">
    /// The runtime member to bind to.
    /// </param>
    /// <param name="replace">
    /// Defines if the bound property shall be replaced.
    /// </param>
    public ObjectFieldBinding(
        MemberInfo member,
        bool replace = true)
    {
        Name = member.Name.EnsureGraphQLName();
        Type = ObjectFieldBindingType.Property;
        Replace = replace;
        Member = member;
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

    /// <summary>
    /// Gets the runtime member this binding refers to, if available.
    /// </summary>
    public MemberInfo? Member { get; }
}
