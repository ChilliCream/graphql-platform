using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Serialization.SchemaDebugFormatter;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// GraphQL interfaces represent a list of named fields and their arguments.
/// GraphQL objects and interfaces can then implement these interfaces
/// which requires that the implementing type will define all fields defined by those
/// interfaces.
/// </para>
/// <para>
/// Fields on a GraphQL interface have the same rules as fields on a GraphQL object;
/// their type can be Scalar, Object, Enum, Interface, or Union, or any wrapping type
/// whose base type is one of those five.
/// </para>
/// <para>
/// For example, an interface NamedEntity may describe a required field and types such
/// as Person or Business may then implement this interface to guarantee this field will
/// always exist.
/// </para>
/// <para>
/// Types may also implement multiple interfaces. For example, Business implements both
/// the NamedEntity and ValuedEntity interfaces in the example below.
/// </para>
/// <code>
/// interface NamedEntity {
///   name: String
/// }
///
/// interface ValuedEntity {
///   value: Int
/// }
///
/// type Person implements NamedEntity {
///   name: String
///   age: Int
/// }
///
/// type Business implements NamedEntity &amp; ValuedEntity {
///   name: String
///   value: Int
///   employeeCount: Int
/// }
/// </code>
/// </summary>
public partial class InterfaceType
    : NamedTypeBase<InterfaceTypeConfiguration>
    , IInterfaceTypeDefinition
{
    /// <summary>
    /// Initializes a new  instance of <see cref="InterfaceType"/>.
    /// </summary>
    protected InterfaceType()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new  instance of <see cref="InterfaceType"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate to specify the properties of this type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public InterfaceType(Action<IInterfaceTypeDescriptor> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <summary>
    /// Create an interface type from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The interface type definition that specifies the properties of the
    /// newly created interface type.
    /// </param>
    /// <returns>
    /// Returns the newly created interface type.
    /// </returns>
    public static InterfaceType CreateUnsafe(InterfaceTypeConfiguration definition)
        => new() { Configuration = definition };

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Interface;

    /// <summary>
    /// Gets the interfaces that are implemented by this type.
    /// </summary>
    public IReadOnlyList<InterfaceType> Implements
        => _implements;

    IReadOnlyInterfaceTypeDefinitionCollection IComplexTypeDefinition.Implements
        => _implements.AsReadOnlyInterfaceTypeDefinitionCollection();

    /// <summary>
    /// Gets the fields of this interface type.
    /// </summary>
    public InterfaceFieldCollection Fields { get; private set; } = null!;

    IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> IComplexTypeDefinition.Fields
        => Fields.AsReadOnlyFieldDefinitionCollection();

    /// <summary>
    /// Defines if this type is implementing an interface
    /// with the given <paramref name="typeName" />.
    /// </summary>
    /// <param name="typeName">
    /// The interface type name.
    /// </param>
    public bool IsImplementing(string typeName)
        => _implements.ContainsName(typeName);

    /// <summary>
    /// Defines if this type is implementing the
    /// the given <paramref name="interfaceType" />.
    /// </summary>
    /// <param name="interfaceType">
    /// The interface type.
    /// </param>
    public bool IsImplementing(InterfaceType interfaceType)
        => IsImplementing(interfaceType.Name);

    bool IComplexTypeDefinition.IsImplementing(IInterfaceTypeDefinition interfaceType)
        => IsImplementing(interfaceType.Name);

    /// <inheritdoc />
    public override bool IsAssignableFrom(ITypeDefinition namedType)
    {
        switch (namedType.Kind)
        {
            case TypeKind.Interface:
                return ReferenceEquals(namedType, this) ||
                    ((InterfaceType)namedType).IsImplementing(this);

            case TypeKind.Object:
                return ((ObjectType)namedType).IsImplementing(this);

            default:
                return false;
        }
    }

    /// <summary>
    /// Resolves the concrete type for the value of a type
    /// that implements this interface.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="resolverResult">
    /// The value for which the type shall be resolved.
    /// </param>
    /// <returns>
    /// Returns <c>null</c> if the value is not of a type
    /// implementing this interface.
    /// </returns>
    public ObjectType? ResolveConcreteType(
        IResolverContext context,
        object resolverResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _resolveAbstractType!.Invoke(context, resolverResult);
    }

    /// <summary>
    /// Override this to configure the type.
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor allows to configure the interface type.
    /// </param>
    protected virtual void Configure(IInterfaceTypeDescriptor descriptor)
    {
    }

    /// <summary>
    /// Creates a <see cref="InterfaceTypeDefinitionNode"/> that represents the interface type.
    /// </summary>
    /// <returns>
    /// The GraphQL syntax node that represents the interface type.
    /// </returns>
    public new InterfaceTypeDefinitionNode ToSyntaxNode()
        => Format(this);

    /// <inheritdoc />
    protected override ITypeDefinitionNode FormatType()
        => Format(this);
}
