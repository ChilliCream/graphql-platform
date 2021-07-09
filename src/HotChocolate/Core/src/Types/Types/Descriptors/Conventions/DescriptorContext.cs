using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    /// <summary>
    /// The descriptor context is passed around during the schema creation and
    /// allows access to conventions and context data.
    /// </summary>
    public sealed class DescriptorContext : IDescriptorContext
    {
        private readonly Dictionary<(Type, string?), IConvention> _conventions = new();
        private readonly IReadOnlyDictionary<(Type, string?), List<CreateConvention>> _cFactories;

        private readonly IServiceProvider _services;

        private INamingConventions? _naming;
        private ITypeInspector? _inspector;

        public event EventHandler<SchemaCompletedEventArgs>? SchemaCompleted;

        private DescriptorContext(
            IReadOnlySchemaOptions options,
            IReadOnlyDictionary<(Type, string?), List<CreateConvention>> conventionFactories,
            IServiceProvider services,
            IDictionary<string, object?> contextData,
            SchemaBuilder.LazySchema schema,
            SchemaInterceptor schemaInterceptor,
            TypeInterceptor typeInterceptor)
        {
            Schema = schema;
            Options = options;
            _cFactories = conventionFactories;
            _services = services;
            ContextData = contextData;
            SchemaInterceptor = schemaInterceptor;
            TypeInterceptor = typeInterceptor;
            ResolverCompiler = new DefaultResolverCompiler(
                services.GetService<IEnumerable<IParameterExpressionBuilder>>());

            schema.Completed += OnSchemaOnCompleted;

            void OnSchemaOnCompleted(object? sender, EventArgs args)
            {
                SchemaCompleted?.Invoke(this, new SchemaCompletedEventArgs(schema.Schema));
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
                                    new XmlDocumentationFileResolver(),
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
        public SchemaInterceptor SchemaInterceptor { get; }

        /// <inheritdoc />
        public TypeInterceptor TypeInterceptor { get; }

        /// <inheritdoc />
        public IResolverCompiler ResolverCompiler { get; }

        /// <inheritdoc />
        public IDictionary<string, object?> ContextData { get; }

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

            if (_conventions.TryGetValue((typeof(T), scope), out IConvention? conv) &&
                conv is T castedConvention)
            {
                return castedConvention;
            }

            CreateConventions<T>(
                scope,
                out IConvention? createdConvention,
                out IList<IConventionExtension>? extensions);

            createdConvention ??= createdConvention as T;
            createdConvention ??= _services.GetService(typeof(T)) as T;
            createdConvention ??= defaultConvention();

            if (createdConvention is Convention init)
            {
                ConventionContext conventionContext =
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

            if (_cFactories.TryGetValue(
                (typeof(T), scope),
                out List<CreateConvention>? factories))
            {
                for (var i = 0; i < factories.Count; i++)
                {
                    IConvention conv = factories[i](_services);
                    if (conv is IConventionExtension extension)
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
                                conv,
                                scope);
                        }

                        createdConvention = conv;
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

        public void Dispose()
        {
            ResolverCompiler.Dispose();
        }

        internal static DescriptorContext Create(
            IReadOnlySchemaOptions? options = null,
            IServiceProvider? services = null,
            IReadOnlyDictionary<(Type, string?), List<CreateConvention>>? conventions = null,
            IDictionary<string, object?>? contextData = null,
            SchemaBuilder.LazySchema? schema = null,
            SchemaInterceptor? schemaInterceptor = null,
            TypeInterceptor? typeInterceptor = null)
        {
            return new(
                options ?? new SchemaOptions(),
                conventions ?? new Dictionary<(Type, string?), List<CreateConvention>>(),
                services ?? new EmptyServiceProvider(),
                contextData ?? new Dictionary<string, object?>(),
                schema ?? new SchemaBuilder.LazySchema(),
                schemaInterceptor ?? new AggregateSchemaInterceptor(),
                typeInterceptor ?? new AggregateTypeInterceptor());
        }

        private class NoOpStringBuilderPool : ObjectPool<StringBuilder>
        {
            public override StringBuilder Get() => new();

            public override void Return(StringBuilder obj)
            {
                obj.Clear();
            }
        }
    }
}
