using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Serialization.SchemaDebugFormatter;

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
    : NamedTypeBase<InputObjectTypeConfiguration>
    , IInputObjectTypeDefinition
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
    public static InputObjectType CreateUnsafe(InputObjectTypeConfiguration definition)
        => new() { Configuration = definition };

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.InputObject;

    /// <summary>
    /// Defines if this input object type is a oneOf input object.
    /// </summary>
    public bool IsOneOf { get; private set; }

    /// <summary>
    /// Gets the fields of this type.
    /// </summary>
    public InputFieldCollection Fields { get; private set; } = null!;

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IInputObjectTypeDefinition.Fields
        => Fields.AsReadOnlyFieldDefinitionCollection();

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

    /// <summary>
    /// Creates a <see cref="InputObjectTypeDefinitionNode"/> that represents the input object type.
    /// </summary>
    /// <returns>
    /// The GraphQL syntax node that represents the input object type.
    /// </returns>
    public new InputObjectTypeDefinitionNode ToSyntaxNode()
        => Format(this);

    /// <inheritdoc />
    protected override ITypeDefinitionNode FormatType()
        => Format(this);
}
