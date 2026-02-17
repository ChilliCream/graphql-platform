using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using ThrowHelper = HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// <para>
/// The descriptor context is passed around during the schema creation and
/// allows access to conventions and context data.
/// </para>
/// <para>
/// Essentially, this is the schema building context.
/// </para>
/// </summary>
public sealed partial class DescriptorContext : IDescriptorContext
{
    private readonly Dictionary<(Type, string?), IConvention> _conventionInstances = [];
    private readonly ServiceHelper _serviceHelper;
    private readonly Func<IReadOnlySchemaOptions> _options;
    private FeatureReference<TypeSystemFeature> _typeSystemFeature = FeatureReference<TypeSystemFeature>.Default;
    private TypeDiscoveryHandler[]? _typeDiscoveryHandlers;

    private DescriptorContext(
        Func<IReadOnlySchemaOptions> options,
        IServiceProvider schemaServices,
        IFeatureCollection features,
        SchemaBuilder.LazySchema schema,
        TypeInterceptor typeInterceptor)
    {
        _options = options;
        Schema = schema;
        Services = schemaServices;
        _serviceHelper = new ServiceHelper(Services);
        Features = features;
        TypeInterceptor = typeInterceptor;

        TypeConverter = _serviceHelper.GetTypeConverter();
        InputFormatter = _serviceHelper.GetInputFormatter(TypeConverter);
        InputParser = _serviceHelper.GetInputParser(TypeConverter);

        TypeInspector = this.GetConventionOrDefault<ITypeInspector>(new DefaultTypeInspector());
        ResolverCompiler = new DefaultResolverCompiler(
            TypeInspector,
            schemaServices,
            _serviceHelper.GetParameterExpressionBuilders());
    }

    internal SchemaBuilder.LazySchema Schema { get; }

    /// <inheritdoc />
    public IServiceProvider Services { get; }

    /// <inheritdoc />
    public IReadOnlySchemaOptions Options => _options();

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public INamingConventions Naming
    {
        get
        {
            field ??= GetConventionOrDefault<INamingConventions>(() => Options.UseXmlDocumentation
                ? new DefaultNamingConventions(
                    new XmlDocumentationProvider(
                        new XmlDocumentationFileResolver(
                            Options.ResolveXmlDocumentationFileName),
                        _serviceHelper.GetStringBuilderPool()))
                : new DefaultNamingConventions(
                    new NoopDocumentationProvider()));

            return field;
        }
    }

    /// <inheritdoc />
    public ITypeInspector TypeInspector { get; }

    /// <inheritdoc />
    public TypeInterceptor TypeInterceptor { get; }

    /// <inheritdoc />
    public IResolverCompiler ResolverCompiler { get; }

    /// <inheritdoc />
    public ITypeConverter TypeConverter { get; }

    /// <inheritdoc />
    public InputParser InputParser { get; }

    /// <inheritdoc />
    public InputFormatter InputFormatter { get; }

    /// <inheritdoc />
    public IList<IDescriptor> Descriptors { get; } = [];

    /// <inheritdoc />
    public INodeIdSerializerAccessor NodeIdSerializerAccessor
        => Services.GetRequiredService<INodeIdSerializerAccessor>();

    /// <inheritdoc />
    public ParameterBindingResolver ParameterBindingResolver
        => Services.GetRequiredService<IRootServiceProviderAccessor>()
            .ServiceProvider.GetRequiredService<ParameterBindingResolver>();

    /// <inheritdoc />
    public IFeatureCollection Features { get; }

    /// <inheritdoc />
    public TypeConfigurationContainer TypeConfiguration { get; } = new();

    /// <inheritdoc />
    public ReadOnlySpan<TypeDiscoveryHandler> GetTypeDiscoveryHandlers()
        => _typeDiscoveryHandlers ??= CreateTypeDiscoveryHandlers();

    /// <inheritdoc />
    public bool TryGetSchemaDirective(
        DirectiveNode directiveNode,
        [NotNullWhen(true)] out ISchemaDirective? directive)
    {
        var feature = _typeSystemFeature.Fetch(Features);

        if (feature is null)
        {
            directive = null;
            return false;
        }

        return feature.SchemaDirectives.TryGetValue(directiveNode.Name.Value, out directive);
    }

    /// <inheritdoc />
    public T GetConventionOrDefault<T>(
        Func<T> factory,
        string? scope = null)
        where T : class, IConvention
    {
        ArgumentNullException.ThrowIfNull(factory);

        var key = (typeof(T), scope);

        if (_conventionInstances.TryGetValue(key, out var convention)
            && convention is T castedConvention)
        {
            return castedConvention;
        }

        CreateConventions<T>(scope, out convention, out var extensions);

        convention ??= convention as T;
        convention ??= _serviceHelper.GetService<T>();
        convention ??= factory();

        if (convention is Convention init)
        {
            var conventionContext = ConventionContext.Create(scope, Services, this);
            init.Initialize(conventionContext);
            MergeExtensions(conventionContext, init, extensions);
            init.Complete(conventionContext);
        }

        if (convention is T createdConventionOfT)
        {
            _conventionInstances[key] = createdConventionOfT;
            return createdConventionOfT;
        }

        throw ThrowHelper.Convention_ConventionCouldNotBeCreated(typeof(T), scope);
    }

    public void OnSchemaCreated(Action<Schema> callback)
        => Schema.OnSchemaCreated(callback);

    private void CreateConventions<T>(
        string? scope,
        out IConvention? convention,
        out List<IConventionExtension>? extensions)
    {
        convention = null;
        extensions = null;

        var feature = Features.Get<TypeSystemConventionFeature>();
        var key = new ConventionKey(typeof(T), scope);

        if (feature is not null
            && feature.Conventions.TryGetValue(key, out var registrations))
        {
            foreach (var registration in registrations)
            {
                var instance = registration.Factory(Services);
                if (instance is IConventionExtension extension)
                {
                    extensions ??= [];
                    extensions.Add(extension);
                }
                else
                {
                    if (convention is not null)
                    {
                        throw ThrowHelper.Convention_TwoConventionsRegisteredForScope(
                            typeof(T),
                            convention,
                            instance,
                            scope);
                    }
                    convention = instance;
                }
            }
        }
    }

    private static void MergeExtensions(
        IConventionContext context,
        Convention convention,
        List<IConventionExtension>? extensions)
    {
        if (extensions is null)
        {
            return;
        }

        foreach (var extension in extensions)
        {
            if (extension is Convention extensionConvention)
            {
                extensionConvention.Initialize(context);
                extension.Merge(context, convention);
                extensionConvention.Complete(context);
            }
        }
    }

    private TypeDiscoveryHandler[] CreateTypeDiscoveryHandlers()
    {
        var feature = _typeSystemFeature.Fetch(Features);

        if (feature?.TypeDiscoveryHandlers.Count > 0)
        {
            var handlers = new TypeDiscoveryHandler[feature.TypeDiscoveryHandlers.Count + 2];

            for (var i = 0; i < feature.TypeDiscoveryHandlers.Count; i++)
            {
                handlers[i] = feature.TypeDiscoveryHandlers[i](this);
            }

            handlers[feature.TypeDiscoveryHandlers.Count] = new ScalarTypeDiscoveryHandler(TypeInspector);
            handlers[feature.TypeDiscoveryHandlers.Count + 1] = new DefaultTypeDiscoveryHandler(TypeInspector);

            return handlers;
        }
        else
        {
            var handlers = new TypeDiscoveryHandler[2];
            handlers[0] = new ScalarTypeDiscoveryHandler(TypeInspector);
            handlers[1] = new DefaultTypeDiscoveryHandler(TypeInspector);
            return handlers;
        }
    }

    public void Dispose() => ResolverCompiler.Dispose();

    internal static DescriptorContext Create(
        IReadOnlySchemaOptions? options = null,
        IServiceProvider? schemaServices = null,
        IFeatureCollection? features = null,
        SchemaBuilder.LazySchema? schema = null,
        TypeInterceptor? typeInterceptor = null)
        => new DescriptorContext(
            () => options ??= new SchemaOptions(),
            schemaServices ?? EmptyServiceProvider.Instance,
            features ?? new FeatureCollection(),
            schema ?? new SchemaBuilder.LazySchema(),
            typeInterceptor ?? new AggregateTypeInterceptor());

    internal static DescriptorContext Create(
        Func<IReadOnlySchemaOptions> options,
        IServiceProvider schemaServices,
        IFeatureCollection? features = null,
        SchemaBuilder.LazySchema? schema = null,
        TypeInterceptor? typeInterceptor = null)
        => new DescriptorContext(
            options,
            schemaServices,
            features ?? new FeatureCollection(),
            schema ?? new SchemaBuilder.LazySchema(),
            typeInterceptor ?? new AggregateTypeInterceptor());
}
