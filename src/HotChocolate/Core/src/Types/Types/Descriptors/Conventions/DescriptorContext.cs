using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    public sealed class DescriptorContext
        : IDescriptorContext
    {
        private readonly IServiceProvider _services;
        private readonly Dictionary<Type, IConvention> _conventions;
        private INamingConventions _naming;
        private ITypeInspector _inspector;

        private DescriptorContext(
            IReadOnlySchemaOptions options,
            IReadOnlyDictionary<Type, IConvention> conventions,
            IServiceProvider services)
        {
            Options = options;
            _conventions = conventions.ToDictionary(t => t.Key, t => t.Value);
            _services = services;
        }

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

        public ITypeInspector Inspector
        {
            get
            {
                if (_inspector is null)
                {
                    _inspector = GetConventionOrDefault<ITypeInspector>(
                        DefaultTypeInspector.Default);
                }
                return _inspector;
            }
        }

        public T GetConventionOrDefault<T>(T defaultConvention)
            where T : class, IConvention =>
            GetConventionOrDefault<T>(() => defaultConvention);

        public T GetConventionOrDefault<T>(Func<T> defaultConvention)
            where T : class, IConvention
        {
            if (defaultConvention is null)
            {
                throw new ArgumentNullException(nameof(defaultConvention));
            }

            if (!TryGetConvention<T>(out T convention))
            {
                convention = _services.GetService(typeof(T)) as T;
            }

            if (convention is null)
            {
                convention = defaultConvention();
                _conventions[typeof(T)] = convention;
            }

            return convention;
        }

        private bool TryGetConvention<T>(out T convention)
            where T : IConvention
        {
            if (_conventions.TryGetValue(typeof(T), out IConvention outConvetion)
                && outConvetion is T conventionOfT)
            {
                convention = conventionOfT;
                return true;
            }
            convention = default;
            return false;
        }

        public static DescriptorContext Create(
            IReadOnlySchemaOptions options,
            IServiceProvider services,
            IReadOnlyDictionary<Type, IConvention> conventions)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            return new DescriptorContext(
                options,
                conventions,
                services);
        }

        public static DescriptorContext Create()
        {
            return new DescriptorContext(
                new SchemaOptions(),
                new Dictionary<Type, IConvention>(),
                new EmptyServiceProvider());
        }
    }
}
