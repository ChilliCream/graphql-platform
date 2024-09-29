using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Factories;
using HotChocolate.Types.Interceptors;
using HotChocolate.Types.Introspection;
using HotChocolate.Types.Pagination;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate;

/// <summary>
/// The schema builder provides a configuration API to create a GraphQL schema.
/// </summary>
public partial class SchemaBuilder : ISchemaBuilder
{
    private delegate TypeReference CreateRef(ITypeInspector typeInspector);

    private readonly Dictionary<string, object?> _contextData = new();
    private readonly List<FieldMiddleware> _globalComponents = [];
    private readonly List<LoadSchemaDocument> _documents = [];
    private readonly List<CreateRef> _types = [];
    private readonly Dictionary<OperationType, CreateRef> _operations = new();
    private readonly Dictionary<(Type, string?), List<CreateConvention>> _conventions = new();
    private readonly Dictionary<Type, (CreateRef, CreateRef)> _clrTypes = new();
    private SchemaFirstTypeInterceptor? _schemaFirstTypeInterceptor;

    private readonly List<object> _typeInterceptors =
    [
        typeof(IntrospectionTypeInterceptor),
        typeof(InterfaceCompletionTypeInterceptor),
        typeof(MiddlewareValidationTypeInterceptor),
        typeof(EnableTrueNullabilityTypeInterceptor),
    ];

    private SchemaOptions _options = new();
    private PagingOptions _pagingOptions = new();
    private IsOfTypeFallback? _isOfType;
    private IServiceProvider? _services;
    private CreateRef? _schema;

    /// <inheritdoc />
    public IDictionary<string, object?> ContextData => _contextData;

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
    public ISchemaBuilder SetSchema(ISchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        if (schema is TypeSystemObjectBase)
        {
            _schema = _ => new SchemaTypeReference(schema);
        }
        else
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilder_ISchemaNotTso,
                nameof(schema));
        }
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
    [Obsolete("Use ModifyOptions instead.")]
    public ISchemaBuilder SetOptions(IReadOnlySchemaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = SchemaOptions.FromOptions(options);
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
    [Obsolete("Use ModifyPagingOptions instead.")]
    public ISchemaBuilder SetPagingOptions(PagingOptions options)
    {
        _pagingOptions = options;
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder ModifyPagingOptions(Action<PagingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_pagingOptions);
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
    public ISchemaBuilder AddDocument(LoadSchemaDocument loadSchemaDocument)
    {
        ArgumentNullException.ThrowIfNull(loadSchemaDocument);

        _schemaFirstTypeInterceptor ??= new SchemaFirstTypeInterceptor();
        _documents.Add(loadSchemaDocument);
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
    public ISchemaBuilder TryAddConvention(
        Type convention,
        CreateConvention factory,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(convention);
        ArgumentNullException.ThrowIfNull(factory);

        if (!_conventions.ContainsKey((convention, scope)))
        {
            AddConvention(convention, factory, scope);
        }

        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder AddConvention(
        Type convention,
        CreateConvention factory,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(convention);

        if (!_conventions.TryGetValue(
            (convention, scope),
            out var factories))
        {
            factories = [];
            _conventions[(convention, scope)] = factories;
        }

        factories.Add(factory);

        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder BindRuntimeType(Type runtimeType, Type schemaType)
    {
        ArgumentNullException.ThrowIfNull(runtimeType);
        ArgumentNullException.ThrowIfNull(schemaType);

        if (!schemaType.IsSchemaType())
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilder_MustBeSchemaType,
                nameof(schemaType));
        }

        var context = SchemaTypeReference.InferTypeContext(schemaType);
        _clrTypes[runtimeType] =
            (ti => ti.GetTypeRef(runtimeType, context),
                ti => ti.GetTypeRef(schemaType, context));

        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder AddType(INamedType namedType)
    {
        ArgumentNullException.ThrowIfNull(namedType);

        _types.Add(_ => TypeReference.Create(namedType));
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder AddType(INamedTypeExtension typeExtension)
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

    /// <inheritdoc />
    public ISchemaBuilder SetContextData(string key, object? value)
    {
        _contextData[key] = value;
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder SetContextData(string key, Func<object?, object?> update)
    {
        _contextData.TryGetValue(key, out var value);
        _contextData[key] = update(value);
        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder TryAddTypeInterceptor(Type interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);

        if (!typeof(TypeInterceptor).IsAssignableFrom(interceptor))
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilder_Interceptor_NotSupported,
                nameof(interceptor));
        }

        if (!_typeInterceptors.Contains(interceptor))
        {
            _typeInterceptors.Add(interceptor);
        }

        return this;
    }

    /// <inheritdoc />
    public ISchemaBuilder TryAddTypeInterceptor(TypeInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);

        if (!_typeInterceptors.Contains(interceptor))
        {
            _typeInterceptors.Add(interceptor);
        }

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
