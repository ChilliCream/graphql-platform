using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public sealed class DescriptorContext
        : IDescriptorContext
    {
        private readonly IServiceProvider _services;
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<Type, IConvention>>
            _conventions;
        private INamingConventions? _naming;
        private ITypeInspector? _inspector;

        private DescriptorContext(
            IReadOnlySchemaOptions options,
            Dictionary<string, IReadOnlyDictionary<Type, IConvention>> conventions,
            IServiceProvider services,
            IDictionary<string, object?> contextData)
        {
            Options = options;
            _conventions = conventions.ToDictionary(t => t.Key, t => t.Value);
            _services = services;
            ContextData = contextData;
        }

        public IReadOnlySchemaOptions Options { get; }

        public INamingConventions Naming
        {
            get
            {
                if (_naming is null)
                {
                    _naming = this.GetConventionOrDefault<INamingConventions>(
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
                    _inspector = this.GetConventionOrDefault<ITypeInspector>(
                        DefaultTypeInspector.Default);
                }
                return _inspector;
            }
        }

        public IDictionary<string, object?> ContextData { get; }

        public T GetConventionOrDefault<T>(
            string? scope,
            Func<T> defaultConvention)
            where T : class, IConvention
        {
            if (defaultConvention is null)
            {
                throw new ArgumentNullException(nameof(defaultConvention));
            }

            if (!TryGetConvention<T>(scope ?? ConventionBase.DefaultScope, out T? convention))
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
            string scope,
            [NotNullWhen(true)] out T? convention)
            where T : class, IConvention
        {
            if (_conventions.TryGetValue(
                    scope, out IReadOnlyDictionary<Type, IConvention>? conventionScope) &&
                conventionScope.TryGetValue(typeof(T), out IConvention outConvention)
                && outConvention is T conventionOfT)
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
            Dictionary<string, IReadOnlyDictionary<Type, IConvention>> conventions,
            IDictionary<string, object?> contextData)
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
                services,
                contextData);
        }

        public static DescriptorContext Create()
        {
            return new DescriptorContext(
                new SchemaOptions(),
                new Dictionary<string, IReadOnlyDictionary<Type, IConvention>>(),
                new EmptyServiceProvider(),
                new Dictionary<string, object?>());
        }
    }
}
