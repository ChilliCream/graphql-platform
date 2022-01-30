using System;

namespace HotChocolate;

/// <summary>
/// Specifies the assembly module name that is being used in combination
/// with the HotChocolate.Types.Analyzers source generators.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ModuleNameAttribute : Attribute
{
    /// <summary>
    /// Initializes new instance of <see cref="ModuleNameAttribute"/>.
    /// </summary>
    /// <param name="name">
    /// The module name.
    /// </param>
    public ModuleNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the module name.
    /// </summary>
    /// <value></value>
    public string Name { get; }
}
