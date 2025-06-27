using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Serialization.SchemaDebugFormatter;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// GraphQL operations are hierarchical and composed, describing a tree of information.
/// While Scalar types describe the leaf values of these hierarchical operations,
/// Objects describe the intermediate levels.
/// </para>
/// <para>
/// GraphQL Objects represent a list of named fields, each of which yield a value of a
/// specific type. Object values should be serialized as ordered maps, where the selected
/// field names (or aliases) are the keys and the result of evaluating the field is the value,
/// ordered by the order in which they appear in the selection set.
/// </para>
/// <para>
/// All fields defined within an Object type must not have a name which begins
/// with "__" (two underscores), as this is used exclusively by
/// GraphQL’s introspection system.
/// </para>
/// </summary>
public partial class ObjectType
    : NamedTypeBase<ObjectTypeConfiguration>
    , IObjectTypeDefinition
{
    /// <summary>
    /// Initializes a new instance of <see cref="ObjectType"/>.
    /// </summary>
    protected ObjectType()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectType"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate to specify the properties of this type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public ObjectType(Action<IObjectTypeDescriptor> configure)
    {
        _configure = configure;
    }

    /// <summary>
    /// Create an object type from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The object type definition that specifies the properties of the
    /// newly created object type.
    /// </param>
    /// <returns>
    /// Returns the newly created object type.
    /// </returns>
    public static ObjectType CreateUnsafe(ObjectTypeConfiguration definition)
        => new() { Configuration = definition };

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Object;

    /// <summary>
    /// Gets the interfaces that are implemented by this type.
    /// </summary>
    public IReadOnlyList<InterfaceType> Implements
        => _implements;

    IReadOnlyInterfaceTypeDefinitionCollection IComplexTypeDefinition.Implements
        => _implements.AsReadOnlyInterfaceTypeDefinitionCollection();

    /// <summary>
    /// Gets the field that this type exposes.
    /// </summary>
    public ObjectFieldCollection Fields { get; private set; } = null!;

    IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> IComplexTypeDefinition.Fields
        => Fields.AsReadOnlyFieldDefinitionCollection();

    /// <inheritdoc />
    public virtual bool IsInstanceOfType(IResolverContext context, object resolverResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(resolverResult);

        return _isOfType!.Invoke(context, resolverResult);
    }

    /// <inheritdoc />
    public bool IsImplementing(string interfaceTypeName)
        => _implements.ContainsName(interfaceTypeName);

    /// <summary>
    /// Defines if this type is implementing the
    /// the given <paramref name="interfaceType" />.
    /// </summary>
    /// <param name="interfaceType">
    /// The interface type.
    /// </param>
    public bool IsImplementing(InterfaceType interfaceType)
        => _implements.ContainsName(interfaceType.Name);

    /// <inheritdoc />
    public bool IsImplementing(IInterfaceTypeDefinition interfaceType)
        => _implements.ContainsName(interfaceType.Name);

    /// <summary>
    /// Override this to configure the type.
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor allows configuring the interface type.
    /// </param>
    protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

    /// <summary>
    /// Creates a <see cref="ObjectTypeDefinitionNode"/> that represents the object type.
    /// </summary>
    /// <returns>
    /// The GraphQL syntax node that represents the object type.
    /// </returns>
    public new ObjectTypeDefinitionNode ToSyntaxNode()
        => Format(this);

    /// <inheritdoc />
    protected override ITypeDefinitionNode FormatType()
        => Format(this);
}
