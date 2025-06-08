using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using static HotChocolate.Serialization.SchemaDebugFormatter;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// GraphQL Unions represent an object that could be one of a list of GraphQL Object types,
/// but provides for no guaranteed fields between those types.
/// </para>
/// <para>
/// They also differ from interfaces in that Object types declare what interfaces
/// they implement, but are not aware of what unions contain them.
/// </para>
/// <para>
/// With interfaces and objects, only those fields defined on the type can be queried directly;
/// to query other fields on an interface, typed fragments must be used.
/// This is the same as for unions, but unions do not define any fields,
/// so no fields may be queried on this type without the use of type refining
/// fragments or inline fragments (with the exception of the meta-field __typename).
/// </para>
/// <para>
/// For example, we might define the following types:
///</para>
/// <code>
/// union SearchResult = Photo | Person
///
/// type Person {
///   name: String
///   age: Int
/// }
///
/// type Photo {
///   height: Int
///   width: Int
/// }
///
/// type SearchQuery {
///   firstSearchResult: SearchResult
/// }
/// </code>
/// </summary>
public class UnionType
    : NamedTypeBase<UnionTypeConfiguration>
    , IUnionTypeDefinition
{
    private const string TypeReference = "typeReference";

    private ObjectTypeCollection _typeMap = null!;
    private Action<IUnionTypeDescriptor>? _configure;
    private ResolveAbstractType? _resolveAbstractType;

    /// <summary>
    /// Initializes a new  instance of <see cref="UnionType"/>.
    /// </summary>
    protected UnionType()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new  instance of <see cref="UnionType"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate to specify the properties of this type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public UnionType(Action<IUnionTypeDescriptor> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <summary>
    /// Create a union type from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The union type definition that specifies the properties of the
    /// newly created union type.
    /// </param>
    /// <returns>
    /// Returns the newly created union type.
    /// </returns>
    public static UnionType CreateUnsafe(UnionTypeConfiguration definition)
        => new() { Configuration = definition };

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Union;

    /// <summary>
    /// Gets the <see cref="ObjectType" /> set of this union type.
    /// </summary>
    public ObjectTypeCollection Types => _typeMap;

    IReadOnlyObjectTypeDefinitionCollection IUnionTypeDefinition.Types
        => Types.AsReadOnlyObjectTypeDefinitionCollection();

    /// <inheritdoc />
    public override bool IsAssignableFrom(ITypeDefinition type)
    {
        switch (type.Kind)
        {
            case TypeKind.Union:
                return ReferenceEquals(type, this);

            case TypeKind.Object:
                return _typeMap.ContainsName(type.Name);

            default:
                return false;
        }
    }

    /// <summary>
    /// Checks if the type set of this union type contains the
    /// specified <paramref name="objectType"/>.
    /// </summary>
    /// <param name="objectType">
    /// The object type.
    /// </param>
    /// <returns>
    /// Returns <c>true</c>, if the type set of this union type contains the
    /// specified <paramref name="objectType"/>; otherwise, <c>false</c> is returned.
    /// </returns>
    public bool ContainsType(ObjectType objectType)
        => _typeMap.ContainsName(objectType.Name);

    /// <inheritdoc />
    public bool ContainsType(string typeName)
        => _typeMap.ContainsName(typeName);

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
    public ObjectType? ResolveConcreteType(IResolverContext context, object resolverResult)
        => _resolveAbstractType?.Invoke(context, resolverResult);

    protected override UnionTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        try
        {
            if (Configuration is null)
            {
                var descriptor = UnionTypeDescriptor.FromSchemaType(
                    context.DescriptorContext,
                    GetType());
                _configure!(descriptor);
                return descriptor.CreateConfiguration();
            }

            return Configuration;
        }
        finally
        {
            _configure = null;
        }
    }

    protected virtual void Configure(IUnionTypeDescriptor descriptor) { }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        UnionTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);

        foreach (var typeRef in configuration.Types)
        {
            context.Dependencies.Add(new(typeRef));
        }

        TypeDependencyHelper.CollectDirectiveDependencies(configuration, context.Dependencies);

        SetTypeIdentity(typeof(UnionType<>));
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        UnionTypeConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);

        CompleteTypeSet(context, configuration);
        CompleteResolveAbstractType(configuration.ResolveAbstractType);
    }

    private void CompleteTypeSet(
        ITypeCompletionContext context,
        UnionTypeConfiguration definition)
    {
        var types = OnCompleteTypeSet(context, definition);

        if (types.Length > 0)
        {
            _typeMap = new ObjectTypeCollection(types);
        }
        else
        {
            _typeMap = ObjectTypeCollection.Empty;
            context.ReportError(SchemaErrorBuilder.New()
                .SetMessage(TypeResources.UnionType_MustHaveTypes)
                .SetCode(ErrorCodes.Schema.MissingType)
                .SetTypeSystemObject(this)
                .Build());
        }
    }

    protected virtual ObjectType[] OnCompleteTypeSet(
        ITypeCompletionContext context,
        UnionTypeConfiguration definition)
    {
        var nameSet = TypeMemHelper.RentNameSet();
        var types = new List<ObjectType>(definition.Types.Count);

        foreach (var typeReference in definition.Types)
        {
            if (context.TryGetType(typeReference, out IType? type))
            {
                if (type.NamedType() is not ObjectType objectType)
                {
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage(
                            "The provided type `{0}` is not an object type and cannot be part of a union type.",
                            type.ToTypeNode().Print())
                        .SetCode(ErrorCodes.Schema.MissingType)
                        .SetTypeSystemObject(this)
                        .SetExtension(TypeReference, typeReference)
                        .Build());
                    continue;
                }

                if (nameSet.Add(objectType.Name))
                {
                    types.Add(objectType);
                }
            }
            else
            {
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(TypeResources.UnionType_UnableToResolveType)
                    .SetCode(ErrorCodes.Schema.MissingType)
                    .SetTypeSystemObject(this)
                    .SetExtension(TypeReference, typeReference)
                    .Build());
            }
        }

        TypeMemHelper.Return(nameSet);
        return [.. types];
    }

    private void CompleteResolveAbstractType(
        ResolveAbstractType? resolveAbstractType)
    {
        if (resolveAbstractType is null)
        {
            // if there is no custom type resolver we will use this default
            // abstract type resolver.
            _resolveAbstractType = (c, r) =>
            {
                foreach (var type in _typeMap)
                {
                    if (type.IsInstanceOfType(c, r))
                    {
                        return type;
                    }
                }

                return null;
            };
        }
        else
        {
            _resolveAbstractType = resolveAbstractType;
        }
    }

    /// <summary>
    /// Creates a <see cref="UnionTypeDefinitionNode"/> that represents the union type.
    /// </summary>
    /// <returns>
    /// The GraphQL syntax node that represents the union type.
    /// </returns>
    public new UnionTypeDefinitionNode ToSyntaxNode()
        => Format(this);

    /// <inheritdoc />
    protected override ITypeDefinitionNode FormatType()
        => Format(this);
}
