using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public sealed class DescriptorContext : IDescriptorContext
    {
        private readonly Dictionary<(Type, string?), IConvention> _conventions =
            new Dictionary<(Type, string?), IConvention>();

        private readonly IReadOnlyDictionary<(Type, string?), List<CreateConvention>>
            _convFactories;

        private readonly IServiceProvider _services;

        private INamingConventions? _naming;
        private ITypeInspector? _inspector;

        public event EventHandler<SchemaCompletedEventArgs>? SchemaCompleted;

        private DescriptorContext(
            IReadOnlySchemaOptions options,
            IReadOnlyDictionary<(Type, string?), List<CreateConvention>> convFactories,
            IServiceProvider services,
            IDictionary<string, object?> contextData,
            SchemaBuilder.LazySchema schema,
            ISchemaInterceptor schemaInterceptor,
            ITypeInterceptor typeInterceptor)
        {
            Options = options;
            _convFactories = convFactories;
            _services = services;
            ContextData = contextData;
            SchemaInterceptor = schemaInterceptor;
            TypeInterceptor = typeInterceptor;

            schema.Completed += OnSchemaOnCompleted;

            void OnSchemaOnCompleted(object sender, EventArgs args)
            {
                SchemaCompleted?.Invoke(this, new SchemaCompletedEventArgs(schema.Schema));
            }
        }

        public IServiceProvider Services => _services;

        public IReadOnlySchemaOptions Options { get; }

        public INamingConventions Naming
        {
            get
            {
                if (_naming is null)
                {
                    _naming = GetConventionOrDefault<INamingConventions>(
                        () => new DefaultNamingConventions(Options.UseXmlDocumentation));
                }

                return _naming;
            }
        }

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

        public ISchemaInterceptor SchemaInterceptor { get; }

        public ITypeInterceptor TypeInterceptor { get; }

        public IDictionary<string, object?> ContextData { get; }

        public T GetConventionOrDefault<T>(
            Func<T> defaultConvention,
            string? scope = null)
            where T : class, IConvention
        {
            if (defaultConvention is null)
            {
                throw new ArgumentNullException(nameof(defaultConvention));
            }

            if (!TryGetConvention(scope, out T? convention))
            {
                convention = _services.GetService(typeof(T)) as T;
            }

            if (convention is null)
            {
                convention = defaultConvention();
            }

            return convention;
        }

        private bool TryGetConvention<T>(
            string? scope,
            [NotNullWhen(true)] out T? convention)
            where T : class, IConvention
        {
            if (_conventions.TryGetValue(
                (typeof(T), scope),
                out IConvention? conv))
            {
                if (conv is T casted)
                {
                    convention = casted;
                    return true;
                }
            }

            if (_convFactories.TryGetValue(
                (typeof(T), scope),
                out List<CreateConvention>? factories))
            {
                Convention? createdConvention = null;
                List<IConventionExtension> extensions = new List<IConventionExtension>();

                for (var i = 0; i < factories.Count; i++)
                {
                    conv = factories[i](_services);
                    if (conv is Convention init)
                    {
                        if (init is IConventionExtension extension)
                        {
                            extensions.Add(extension);
                        }
                        else
                        {
                            if (createdConvention is null)
                            {
                                createdConvention = init;
                            }
                            else
                            {
                                throw ThrowHelper.Convention_TwoConventionsRegisteredForScope(
                                    typeof(T),
                                    createdConvention,
                                    init,
                                    scope);
                            }
                        }
                    }
                }

                if (createdConvention is {})
                {
                    ConventionContext conventionContext =
                        ConventionContext.Create(scope, _services, this);

                    createdConvention.Initialize(conventionContext);
                    MergeExtensions(conventionContext, createdConvention, extensions);
                    createdConvention.OnComplete(conventionContext);

                    _conventions[(typeof(T), scope)] = createdConvention;
                }

                if (createdConvention is T casted)
                {
                    convention = casted;
                    return true;
                }
            }

            convention = default;
            return false;
        }

        private static void MergeExtensions(
            IConventionContext context,
            Convention convention,
            IList<IConventionExtension> extensions)
        {
            for (var m = 0; m < extensions.Count; m++)
            {
                if (extensions[m] is Convention extensionConvention)
                {
                    extensionConvention.Initialize(context);
                    extensions[m].Merge(context, convention);
                    extensionConvention.OnComplete(context);
                }
            }
        }

        internal static DescriptorContext Create(
            IReadOnlySchemaOptions? options = null,
            IServiceProvider? services = null,
            IReadOnlyDictionary<(Type, string?), List<CreateConvention>>? conventions = null,
            IDictionary<string, object?>? contextData = null,
            SchemaBuilder.LazySchema? schema = null,
            ISchemaInterceptor? schemaInterceptor = null,
            ITypeInterceptor? typeInterceptor = null)
        {
            return new DescriptorContext(
                options ?? new SchemaOptions(),
                conventions ?? new Dictionary<(Type, string?), List<CreateConvention>>(),
                services ?? new EmptyServiceProvider(),
                contextData ?? new Dictionary<string, object?>(),
                schema ?? new SchemaBuilder.LazySchema(),
                schemaInterceptor ?? new AggregateSchemaInterceptor(),
                typeInterceptor ?? new AggregateTypeInterceptor());
        }
    }
}
