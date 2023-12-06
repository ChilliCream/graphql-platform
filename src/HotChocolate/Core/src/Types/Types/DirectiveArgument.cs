#nullable enable
using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

/// <summary>
/// Represents a directive argument.
/// </summary>
public sealed class DirectiveArgument : Argument, IHasProperty
{
    /// <summary>
    /// Initializes a new <see cref="DirectiveArgument"/>.
    /// </summary>
    /// <param name="definition">
    /// The argument definition.
    /// </param>
    /// <param name="index">
    /// The position of the argument within the field collection.
    /// </param>
    public DirectiveArgument(DirectiveArgumentDefinition definition, int index)
        : base(definition, index)
    {
        Property = definition.Property;
    }

    /// <summary>
    /// Gets the property this argument is bound to.
    /// </summary>
    public PropertyInfo? Property { get; }
}
