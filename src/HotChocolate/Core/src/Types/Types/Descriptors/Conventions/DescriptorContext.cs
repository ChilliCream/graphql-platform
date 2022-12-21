using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.WellKnownContextData;
using ThrowHelper = HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// The descriptor context is passed around during the schema creation and
/// allows access to conventions and context data.
///
/// Essentially this is the schema building context.
/// </summary>
public sealed class DescriptorContext : IDescriptorContext
{
    private readonly Dictionary<(Type, string?), IConvention> _conventions = new();
    private readonly IReadOnlyDictionary<(Type, string?), List<CreateConvention>> _cFactories;
    private readonly Dictionary<string, ISchemaDirective> _schemaDirectives = new();

    private readonly IServiceProvider _services;

    private TypeDiscoveryHandler[]? _typeDiscoveryHandlers;
    private INamingConventions? _naming;
    private ITypeInspector? _inspector;

    public event EventHandler<SchemaCompletedEventArgs>? SchemaCompleted;

    private DescriptorContext(
        IReadOnlySchemaOptions options,
        IReadOnlyDictionary<(Type, string?), List<CreateConvention>> conventionFactories,
        IServiceProvider services,
        IDictionary<string, object?> contextData,
        SchemaBuilder.LazySchema schema,
        TypeInterceptor typeInterceptor)
    {
        Schema = schema;
        Options = options;
        _cFactories = conventionFactories;
        _services = services;
        ContextData = contextData;
        TypeInterceptor = typeInterceptor;
        ResolverCompiler = new DefaultResolverCompiler(
            services.GetService<IEnumerable<IParameterExpressionBuilder>>());

        var typeConverter = Services.GetTypeConverter();
        InputParser = services.GetService<InputParser>() ?? new InputParser(typeConverter);
        InputFormatter = services.GetService<InputFormatter>() ?? new InputFormatter(typeConverter);

        schema.Completed += OnSchemaOnCompleted;

        void OnSchemaOnCompleted(object? sender, EventArgs args)
        {
            SchemaCompleted?.Invoke(this, new SchemaCompletedEventArgs(schema.Schema));
            SchemaCompleted = null;
        }
    }

    internal SchemaBuilder.LazySchema Schema { get; }

    /// <inheritdoc />
    public IServiceProvider Services => _services;

    /// <inheritdoc />
    public IReadOnlySchemaOptions Options { get; }

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
                                Services.GetService<ObjectPool<StringBuilder>>() ??
                                    new NoOpStringBuilderPool()))
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
    public InputParser InputParser { get; }

    /// <inheritdoc />
    public InputFormatter InputFormatter { get; }

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

        if (_conventions.TryGetValue((typeof(T), scope), out var conv) &&
            conv is T castedConvention)
        {
            return castedConvention;
        }

        CreateConventions<T>(
            scope,
            out var createdConvention,
            out var extensions);

        createdConvention ??= createdConvention as T;
        createdConvention ??= _services.GetService(typeof(T)) as T;
        createdConvention ??= defaultConvention();

        if (createdConvention is Convention init)
        {
            var conventionContext =
                ConventionContext.Create(scope, _services, this);

            init.Initialize(conventionContext);
            MergeExtensions(conventionContext, init, extensions);
            init.Complete(conventionContext);
        }

        if (createdConvention is T createdConventionOfT)
        {
            _conventions[(typeof(T), scope)] = createdConventionOfT;
            return createdConventionOfT;
        }

        throw ThrowHelper.Convention_ConventionCouldNotBeCreated(typeof(T), scope);
    }

    private void CreateConventions<T>(
        string? scope,
        out IConvention? createdConvention,
        out IList<IConventionExtension> extensions)
    {
        createdConvention = null;
        extensions = new List<IConventionExtension>();

        if (_cFactories.TryGetValue((typeof(T), scope), out var factories))
        {
            for (var i = 0; i < factories.Count; i++)
            {
                var convention = factories[i](_services);
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
            value is IReadOnlyList<Func<IDescriptorContext, TypeDiscoveryHandler>> { Count: > 0 } h)
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
    {
        return new(
            options ?? new SchemaOptions(),
            conventions ?? new Dictionary<(Type, string?), List<CreateConvention>>(),
            services ?? new EmptyServiceProvider(),
            contextData ?? new Dictionary<string, object?>(),
            schema ?? new SchemaBuilder.LazySchema(),
            typeInterceptor ?? new AggregateTypeInterceptor());
    }

    private sealed class NoOpStringBuilderPool : ObjectPool<StringBuilder>
    {
        public override StringBuilder Get() => new();

        public override void Return(StringBuilder obj)
        {
            obj.Clear();
        }
    }
}
