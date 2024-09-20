using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using static HotChocolate.WellKnownContextData;
using ThrowHelper = HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// <para>
/// The descriptor context is passed around during the schema creation and
/// allows access to conventions and context data.
/// </para>
/// <para>Essentially this is the schema building context.</para>
/// </summary>
public sealed partial class DescriptorContext : IDescriptorContext
{
    private readonly Dictionary<(Type, string?), IConvention> _conventionInstances = new();
    private readonly IReadOnlyDictionary<(Type, string?), List<CreateConvention>> _conventions;
    private readonly Dictionary<string, ISchemaDirective> _schemaDirectives = new();
    private readonly IServiceProvider _schemaServices;
    private readonly ServiceHelper _serviceHelper;
    private readonly Func<IReadOnlySchemaOptions> _options;

    private TypeDiscoveryHandler[]? _typeDiscoveryHandlers;
    private INamingConventions? _naming;
    private ITypeInspector? _inspector;

    private DescriptorContext(
        Func<IReadOnlySchemaOptions> options,
        IReadOnlyDictionary<(Type, string?), List<CreateConvention>> conventions,
        IServiceProvider schemaServices,
        IDictionary<string, object?> contextData,
        SchemaBuilder.LazySchema schema,
        TypeInterceptor typeInterceptor)
    {
        _options = options;
        Schema = schema;
        _conventions = conventions;
        _schemaServices = schemaServices;
        _serviceHelper = new ServiceHelper(_schemaServices);
        ContextData = contextData;
        TypeInterceptor = typeInterceptor;
        ResolverCompiler = new DefaultResolverCompiler(
            schemaServices,
            _serviceHelper.GetParameterExpressionBuilders());

        TypeConverter = _serviceHelper.GetTypeConverter();
        InputFormatter = _serviceHelper.GetInputFormatter(TypeConverter);
        InputParser = _serviceHelper.GetInputParser(TypeConverter);
    }

    internal SchemaBuilder.LazySchema Schema { get; }

    /// <inheritdoc />
    public IServiceProvider Services => _schemaServices;

    /// <inheritdoc />
    public IReadOnlySchemaOptions Options => _options();

    /// <inheritdoc />
    public INamingConventions Naming
    {
        get
        {
            if (_naming is null)
            {
                _naming = GetConventionOrDefault<INamingConventions>(
                    () => Options.UseXmlDocumentation
                        ? new DefaultNamingConventions(
                            new XmlDocumentationProvider(
                                new XmlDocumentationFileResolver(
                                    Options.ResolveXmlDocumentationFileName),
                                _serviceHelper.GetStringBuilderPool()))
                        : new DefaultNamingConventions(
                            new NoopDocumentationProvider()));
            }

            return _naming;
        }
    }

    /// <inheritdoc />
    public ITypeInspector TypeInspector
    {
        get
        {
            if (_inspector is null)
            {
                _inspector = this.GetConventionOrDefault<ITypeInspector>(
                    new DefaultTypeInspector());
            }

            return _inspector;
        }
    }

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
    public IList<IDescriptor> Descriptors { get; } = new List<IDescriptor>();

    /// <inheritdoc />
    public INodeIdSerializerAccessor NodeIdSerializerAccessor
        => _schemaServices.GetRequiredService<INodeIdSerializerAccessor>();

    /// <inheritdoc />
    public IParameterBindingResolver ParameterBindingResolver
        => Services.GetRequiredService<IApplicationServiceProvider>().GetRequiredService<IParameterBindingResolver>();

    /// <inheritdoc />
    public IDictionary<string, object?> ContextData { get; }

    /// <inheritdoc />
    public ReadOnlySpan<TypeDiscoveryHandler> GetTypeDiscoveryHandlers()
        => _typeDiscoveryHandlers ??= CreateTypeDiscoveryHandlers(this);

    /// <inheritdoc />
    public bool TryGetSchemaDirective(
        DirectiveNode directiveNode,
        [NotNullWhen(true)] out ISchemaDirective? directive)
    {
        if (ContextData.TryGetValue(SchemaDirectives, out var value) &&
            value is IReadOnlyList<ISchemaDirective> directives)
        {
            foreach (var sd in directives)
            {
                _schemaDirectives[sd.Name] = sd;
            }

            ContextData.Remove(SchemaDirectives);
        }

        return _schemaDirectives.TryGetValue(directiveNode.Name.Value, out directive);
    }

    /// <inheritdoc />
    public T GetConventionOrDefault<T>(
        Func<T> defaultConvention,
        string? scope = null)
        where T : class, IConvention
    {
        if (defaultConvention is null)
        {
            throw new ArgumentNullException(nameof(defaultConvention));
        }

        var key = (typeof(T), scope);

        if (_conventionInstances.TryGetValue(key, out var convention) &&
            convention is T castedConvention)
        {
            return castedConvention;
        }

        CreateConventions<T>(scope, out var createdConvention, out var extensions);

        createdConvention ??= createdConvention as T;
        createdConvention ??= _serviceHelper.GetService<T>();
        createdConvention ??= defaultConvention();

        if (createdConvention is Convention init)
        {
            var conventionContext = ConventionContext.Create(scope, _schemaServices, this);
            init.Initialize(conventionContext);
            MergeExtensions(conventionContext, init, extensions);
            init.Complete(conventionContext);
        }

        if (createdConvention is T createdConventionOfT)
        {
            _conventionInstances[key] = createdConventionOfT;
            return createdConventionOfT;
        }

        throw ThrowHelper.Convention_ConventionCouldNotBeCreated(typeof(T), scope);
    }

    public void OnSchemaCreated(Action<ISchema> callback)
        => Schema.OnSchemaCreated(callback);

    private void CreateConventions<T>(
        string? scope,
        out IConvention? createdConvention,
        out IList<IConventionExtension> extensions)
    {
        createdConvention = null;
        extensions = new List<IConventionExtension>();

        if (_conventions.TryGetValue((typeof(T), scope), out var factories))
        {
            for (var i = 0; i < factories.Count; i++)
            {
                var convention = factories[i](_schemaServices);
                if (convention is IConventionExtension extension)
                {
                    extensions.Add(extension);
                }
                else
                {
                    if (createdConvention is not null)
                    {
                        throw ThrowHelper.Convention_TwoConventionsRegisteredForScope(
                            typeof(T),
                            createdConvention,
                            convention,
                            scope);
                    }

                    createdConvention = convention;
                }
            }
        }
    }

    private static void MergeExtensions(
        IConventionContext context,
        Convention convention,
        IList<IConventionExtension> extensions)
    {
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

    private static TypeDiscoveryHandler[] CreateTypeDiscoveryHandlers(
        IDescriptorContext self)
    {
        TypeDiscoveryHandler[] array;

        if (self.ContextData.TryGetValue(TypeDiscoveryHandlers, out var value) &&
            value is IReadOnlyList<Func<IDescriptorContext, TypeDiscoveryHandler>> { Count: > 0, } h)
        {
            array = new TypeDiscoveryHandler[h.Count + 2];

            for (var i = 0; i < h.Count; i++)
            {
                array[i] = h[i](self);
            }

            array[h.Count] = new ScalarTypeDiscoveryHandler(self.TypeInspector);
            array[h.Count + 1] = new DefaultTypeDiscoveryHandler(self.TypeInspector);
        }
        else
        {
            array = new TypeDiscoveryHandler[2];
            array[0] = new ScalarTypeDiscoveryHandler(self.TypeInspector);
            array[1] = new DefaultTypeDiscoveryHandler(self.TypeInspector);
        }

        return array;
    }

    public void Dispose() => ResolverCompiler.Dispose();

    internal static DescriptorContext Create(
        IReadOnlySchemaOptions? options = null,
        IServiceProvider? services = null,
        IReadOnlyDictionary<(Type, string?), List<CreateConvention>>? conventions = null,
        IDictionary<string, object?>? contextData = null,
        SchemaBuilder.LazySchema? schema = null,
        TypeInterceptor? typeInterceptor = null)
        => new DescriptorContext(
            () => (options ??= new SchemaOptions()),
            conventions ?? new Dictionary<(Type, string?), List<CreateConvention>>(),
            services ?? EmptyServiceProvider.Instance,
            contextData ?? new Dictionary<string, object?>(),
            schema ?? new SchemaBuilder.LazySchema(),
            typeInterceptor ?? new AggregateTypeInterceptor());

    internal static DescriptorContext Create(
        Func<IReadOnlySchemaOptions> options,
        IServiceProvider services,
        IReadOnlyDictionary<(Type, string?), List<CreateConvention>>? conventions = null,
        IDictionary<string, object?>? contextData = null,
        SchemaBuilder.LazySchema? schema = null,
        TypeInterceptor? typeInterceptor = null)
        => new DescriptorContext(
            options,
            conventions ?? new Dictionary<(Type, string?), List<CreateConvention>>(),
            services,
            contextData ?? new Dictionary<string, object?>(),
            schema ?? new SchemaBuilder.LazySchema(),
            typeInterceptor ?? new AggregateTypeInterceptor());
}
