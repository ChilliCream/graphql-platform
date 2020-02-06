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
        private readonly ConventionCache _conventionCache;
        private INamingConventions _naming;
        private ITypeInspector _inspector;

        private DescriptorContext(
            IReadOnlySchemaOptions options,
            ConventionCache conventionCache,
            IServiceProvider services)
        {
            Options = options;
            _conventionCache = conventionCache;
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
            GetConventionOrDefault<T>(Convention.DefaultName, defaultConvention);

        public T GetConventionOrDefault<T>(string name, T defaultConvention)
            where T : class, IConvention =>
            GetConventionOrDefault<T>(name, () => defaultConvention);

        public T GetConventionOrDefault<T>(Func<T> defaultConvention)
            where T : class, IConvention =>
            GetConventionOrDefault<T>(Convention.DefaultName, defaultConvention);

        public T GetConventionOrDefault<T>(string name, Func<T> defaultConvention)
            where T : class, IConvention
        {
            if (defaultConvention is null)
            {
                throw new ArgumentNullException(nameof(defaultConvention));
            }

            return _conventionCache.GetOrAdd<T>(name, (sp) =>
                {
                    // TODO: add test case!
                    // Conventions that are registered with dependency injection are only allowed
                    // in default 
                    if (name != Convention.DefaultName ||
                        !(sp.GetService(typeof(T)) is T convention))
                    {
                        convention = defaultConvention();
                    }
                    return convention;
                });
        }

        public static DescriptorContext Create(
            IReadOnlySchemaOptions options,
            IServiceProvider services,
            IEnumerable<ConfigureNamedConvention> conventions)
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
                ConventionCache.Create(services, conventions),
                services);
        }

        public static DescriptorContext Create()
        {
            var services = new EmptyServiceProvider();
            return new DescriptorContext(
                new SchemaOptions(),
                ConventionCache.Create(services, Enumerable.Empty<ConfigureNamedConvention>()),
                services);
        }
    }
}
