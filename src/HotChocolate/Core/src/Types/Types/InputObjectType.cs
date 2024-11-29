using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// A GraphQL Input Object defines a set of input fields; the input fields are either scalars,
/// enums, or other input objects. This allows arguments to accept arbitrarily complex structs.
/// </para>
/// <para>In this example, an Input Object called Point2D describes x and y inputs:</para>
///
/// <code>
/// input Point2D {
///   x: Float
///   y: Float
/// }
/// </code>
/// </summary>
public partial class InputObjectType
    : NamedTypeBase<InputObjectTypeDefinition>
    , IInputObjectType
{
    /// <summary>
    /// Initializes a new  instance of <see cref="InputObjectType"/>.
    /// </summary>
    protected InputObjectType()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new  instance of <see cref="InputObjectType"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate to specify the properties of this type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public InputObjectType(Action<IInputObjectTypeDescriptor> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <summary>
    /// Create an input object type from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The input object type definition that specifies the properties of the
    /// newly created input object type.
    /// </param>
    /// <returns>
    /// Returns the newly created input object type.
    /// </returns>
    public static InputObjectType CreateUnsafe(InputObjectTypeDefinition definition)
        => new() { Definition = definition, };

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.InputObject;

    /// <summary>
    /// Gets the fields of this type.
    /// </summary>
    public FieldCollection<InputField> Fields { get; private set; } = default!;

    IFieldCollection<IInputField> IInputObjectType.Fields => Fields;

    internal object CreateInstance(object?[] fieldValues)
        => _createInstance(fieldValues);

    internal void GetFieldValues(object runtimeValue, object?[] fieldValues)
        => _getFieldValues(runtimeValue, fieldValues);

    /// <summary>
    /// Override this to configure the type.
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor allows to configure the interface type.
    /// </param>
    protected virtual void Configure(IInputObjectTypeDescriptor descriptor)
    {
    }
}
