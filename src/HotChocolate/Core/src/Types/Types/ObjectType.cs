using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// GraphQL operations are hierarchical and composed, describing a tree of information.
/// While Scalar types describe the leaf values of these hierarchical operations,
/// Objects describe the intermediate levels.
///
/// GraphQL Objects represent a list of named fields, each of which yield a value of a
/// specific type. Object values should be serialized as ordered maps, where the selected
/// field names (or aliases) are the keys and the result of evaluating the field is the value,
/// ordered by the order in which they appear in the selection set.
///
/// All fields defined within an Object type must not have a name which begins
/// with "__" (two underscores), as this is used exclusively by
/// GraphQL’s introspection system.
/// </summary>
public partial class ObjectType
    : NamedTypeBase<ObjectTypeDefinition>
    , IObjectType
{
    /// <summary>
    /// Initializes a new  instance of <see cref="ObjectType"/>.
    /// </summary>
    protected ObjectType()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new  instance of <see cref="ObjectType"/>.
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
    /// Create a object type from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The object type definition that specifies the properties of the
    /// newly created object type.
    /// </param>
    /// <returns>
    /// Returns the newly created object type.
    /// </returns>
    public static ObjectType CreateUnsafe(ObjectTypeDefinition definition)
        => new() { Definition = definition, };

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Object;

    /// <summary>
    /// Gets the interfaces that are implemented by this type.
    /// </summary>
    public IReadOnlyList<InterfaceType> Implements => _implements;

    IReadOnlyList<IInterfaceType> IComplexOutputType.Implements => Implements;

    /// <summary>
    /// Gets the field that this type exposes.
    /// </summary>
    public FieldCollection<ObjectField> Fields { get; private set; } = default!;

    IFieldCollection<IObjectField> IObjectType.Fields => Fields;

    IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

    /// <inheritdoc />
    public virtual bool IsInstanceOfType(IResolverContext context, object resolverResult)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (resolverResult is null)
        {
            throw new ArgumentNullException(nameof(resolverResult));
        }

        return _isOfType!.Invoke(context, resolverResult);
    }

    /// <inheritdoc />
    public bool IsImplementing(string interfaceTypeName)
    {
        if (string.IsNullOrEmpty(interfaceTypeName))
        {
            throw new ArgumentNullException(nameof(interfaceTypeName));
        }

        for (var i = 0; i < _implements.Length; i++)
        {
            if (interfaceTypeName.EqualsOrdinal(_implements[i].Name))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Defines if this type is implementing the
    /// the given <paramref name="interfaceType" />.
    /// </summary>
    /// <param name="interfaceType">
    /// The interface type.
    /// </param>
    public bool IsImplementing(InterfaceType interfaceType)
        => Array.IndexOf(_implements, interfaceType) != -1;

    /// <inheritdoc />
    public bool IsImplementing(IInterfaceType interfaceType)
        => interfaceType is InterfaceType casted && IsImplementing(casted);

    /// <summary>
    /// Override this to configure the type.
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor allows to configure the interface type.
    /// </param>
    protected virtual void Configure(IObjectTypeDescriptor descriptor) { }
}
