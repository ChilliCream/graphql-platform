using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public sealed class DescriptorContext
        : IDescriptorContext
    {
        private readonly Dictionary<(Type, string?), IConvention> _conventions =
            new Dictionary<(Type, string?), IConvention>();
        private readonly IReadOnlyDictionary<(Type, string?), CreateConvention> _convFactories;
        private readonly IServiceProvider _services;

        private INamingConventions? _naming;
        private ITypeInspector? _inspector;

        public event EventHandler<SchemaCompletedEventArgs>? SchemaCompleted;

        private DescriptorContext(
            IReadOnlySchemaOptions options,
            IReadOnlyDictionary<(Type, string?), CreateConvention> convFactories,
            IServiceProvider services,
            IDictionary<string, object?> contextData,
            SchemaBuilder.LazySchema schema)
        {
            Options = options;
            _convFactories = convFactories;
            _services = services;
            ContextData = contextData;
            schema.Completed += (sender, args) =>
                SchemaCompleted?.Invoke(this, new SchemaCompletedEventArgs(schema.Schema));
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
                (typeof(T), scope), out IConvention? conv))
            {
                if (conv is T casted)
                {
                    convention = casted;
                    return true;
                }
            }

            if (_convFactories.TryGetValue(
                (typeof(T), scope),
                out CreateConvention? factory))
            {
                conv = factory(_services);
                if (conv is Convention init)
                {
                    var conventionContext = new ConventionContext(init, scope, _services, this);
                    init.Initialize(conventionContext);
                    _conventions[(typeof(T), scope)] = init;
                }

                if (conv is T casted)
                {
                    convention = casted;
                    return true;
                }
            }

            convention = default;
            return false;
        }

        internal static DescriptorContext Create(
            IReadOnlySchemaOptions options,
            IServiceProvider services,
            IReadOnlyDictionary<(Type, string?), CreateConvention> conventions,
            IDictionary<string, object?> contextData,
            SchemaBuilder.LazySchema schema)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (conventions is null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            return new DescriptorContext(options, conventions, services, contextData, schema);
        }

        internal static DescriptorContext Create()
        {
            return new DescriptorContext(
                new SchemaOptions(),
                new Dictionary<(Type, string?), CreateConvention>(),
                new EmptyServiceProvider(),
                new Dictionary<string, object?>(),
                new SchemaBuilder.LazySchema());
        }
    }
}
