using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// GraphQL Enum types, like Scalar types, also represent leaf values in a GraphQL type system.
/// However Enum types describe the set of possible values.
///
/// Enums are not references for a numeric value, but are unique values in their own right.
/// They may serialize as a string: the name of the represented value.
///
/// In this example, an Enum type called Direction is defined:
///
/// <code>
/// enum Direction {
///   NORTH
///   EAST
///   SOUTH
///   WEST
/// }
/// </code>
/// </summary>
public class EnumType<T> : EnumType, IEnumType<T>
{
    private Action<IEnumTypeDescriptor<T>>? _configure;

    /// <summary>
    /// Initializes a new instance of <see cref="EnumType"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate defining the configuration.
    /// </param>
    public EnumType(Action<IEnumTypeDescriptor<T>> configure)
    {
        _configure = configure
            ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EnumType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public EnumType()
    {
        _configure = Configure;
    }

    /// <inheritdoc />
    public bool TryGetRuntimeValue(string name, [NotNullWhen(true)] out T runtimeValue)
    {
        if (base.TryGetRuntimeValue(name, out var rv) &&
            rv is T casted)
        {
            runtimeValue = casted;
            return true;
        }

        runtimeValue = default!;
        return false;
    }

    /// <summary>
    /// Override this in order to specify the type configuration explicitly.
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor of this type lets you express the type configuration.
    /// </param>
    protected virtual void Configure(IEnumTypeDescriptor<T> descriptor) { }

    protected sealed override void Configure(IEnumTypeDescriptor descriptor)
        => throw new NotSupportedException();

    /// <inheritdoc />
    protected override EnumTypeDefinition CreateDefinition(
        ITypeDiscoveryContext context)
    {
        var descriptor =
            EnumTypeDescriptor.New<T>(context.DescriptorContext);

        _configure!(descriptor);
        _configure = null;

        return descriptor.CreateDefinition();
    }
}
