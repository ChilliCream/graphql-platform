using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types;

/// <summary>
/// Represents a directive argument.
/// </summary>
public sealed class DirectiveArgument : Argument, IPropertyProvider
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
    public DirectiveArgument(DirectiveArgumentConfiguration definition, int index)
        : base(definition, index)
    {
        Property = definition.Property;
    }

    /// <summary>
    /// Gets the directive type that declares this argument.
    /// </summary>
    public new DirectiveType DeclaringType => Unsafe.As<DirectiveType>(base.DeclaringType);

    /// <summary>
    /// Gets the property this argument is bound to.
    /// </summary>
    public PropertyInfo? Property { get; }
}
