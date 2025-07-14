using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Interceptors;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate;

/// <summary>
/// The schema builder provides a configuration API to create a GraphQL schema.
/// </summary>
public partial class SchemaBuilder : ISchemaBuilder
{
    private delegate TypeReference CreateRef(ITypeInspector typeInspector);
    private readonly List<FieldMiddleware> _globalComponents = [];
    private readonly List<CreateRef> _types = [];
    private readonly Dictionary<OperationType, CreateRef> _operations = [];

    private readonly SchemaOptions _options = new();
    private IsOfTypeFallback? _isOfType;
    private IServiceProvider? _services;
    private CreateRef? _schema;

    private SchemaBuilder()
    {
        var typeInterceptors = new TypeInterceptorCollection();

        typeInterceptors.TryAdd(new IntrospectionTypeInterceptor());
        typeInterceptors.TryAdd(new InterfaceCompletionTypeInterceptor());
        typeInterceptors.TryAdd(new MiddlewareValidationTypeInterceptor());
        typeInterceptors.TryAdd(new SemanticNonNullTypeInterceptor());
        typeInterceptors.TryAdd(new StoreGlobalPagingOptionsTypeInterceptor());

        Features.Set(typeInterceptors);
    }

    public IFeatureCollection Features { get; } = new FeatureCollection();

    /// <inheritdoc />
    public ISchemaBuilder SetSchema(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (typeof(Schema).IsAssignableFrom(type))
        {
            _schema = ti => ti.GetTypeRef(type);
        }
        else
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilder_SchemaTypeInvalid,
                nameof(type));
        }
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder SetSchema(Schema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        _schema = _ => new SchemaTypeReference(schema);
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder SetSchema(Action<ISchemaTypeDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _schema = _ => new SchemaTypeReference(new Schema(configure));
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder ModifyOptions(Action<SchemaOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_options);
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder Use(FieldMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);

        _globalComponents.Add(middleware);
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder AddType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        _types.Add(ti => ti.GetTypeRef(type));

        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder AddType(ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        _types.Add(_ => TypeReference.Create(type));
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder AddType(ITypeDefinitionExtension typeExtension)
    {
        ArgumentNullException.ThrowIfNull(typeExtension);

        _types.Add(_ => TypeReference.Create(typeExtension));
        return this;
    }

    internal void AddTypeReference(TypeReference typeReference)
    {
        ArgumentNullException.ThrowIfNull(typeReference);

        _types.Add(_ => typeReference);
    }

    /// <inheritdoc />
    public ISchemaBuilder AddDirectiveType(DirectiveType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        _types.Add(_ => TypeReference.Create(type));
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder AddRootType(Type rootType, OperationType operation)
    {
        ArgumentNullException.ThrowIfNull(rootType);

        if (!rootType.IsClass)
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilder_RootType_MustBeClass,
                nameof(rootType));
        }

        if (rootType.IsNonGenericSchemaType())
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilder_RootType_NonGenericType,
                nameof(rootType));
        }

        if (rootType.IsSchemaType()
            && !typeof(ObjectType).IsAssignableFrom(rootType))
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilder_RootType_MustBeObjectType,
                nameof(rootType));
        }

        if (_operations.ContainsKey(operation))
        {
            throw new ArgumentException(
                string.Format(
                    TypeResources.SchemaBuilder_AddRootType_TypeAlreadyRegistered,
                    operation),
                nameof(operation));
        }

        _operations.Add(operation, ti => ti.GetTypeRef(rootType, TypeContext.Output));
        _types.Add(ti => ti.GetTypeRef(rootType, TypeContext.Output));
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder AddRootType(ObjectType rootType, OperationType operation)
    {
        ArgumentNullException.ThrowIfNull(rootType);

        if (_operations.ContainsKey(operation))
        {
            throw new ArgumentException(
                string.Format(
                    TypeResources.SchemaBuilder_AddRootType_TypeAlreadyRegistered,
                    operation),
                nameof(operation));
        }

        var reference = TypeReference.Create(rootType);
        _operations.Add(operation, _ => reference);
        _types.Add(_ => reference);
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder TryAddRootType(Func<ObjectType> rootType, OperationType operation)
    {
        ArgumentNullException.ThrowIfNull(rootType);

        if (_operations.ContainsKey(operation))
        {
            return this;
        }

        var reference = TypeReference.Create(rootType());
        _operations.Add(operation, _ => reference);
        _types.Add(_ => reference);
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder SetTypeResolver(IsOfTypeFallback isOfType)
    {
        _isOfType = isOfType ?? throw new ArgumentNullException(nameof(isOfType));
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder AddServices(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = _services is null ? services : new CombinedServiceProvider(_services, services);

        return this;
    }

    /// <summary>
    /// Creates a new <see cref="SchemaBuilder"/> instance.
    /// </summary>
    /// <returns>
    /// Returns a new instance of <see cref="SchemaBuilder"/>.
    /// </returns>
    public static SchemaBuilder New() => new();
}
