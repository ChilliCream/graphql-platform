using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    internal class ConventionCache
    {
        private readonly IServiceProvider _services;
        private readonly ConcurrentDictionary<Type,
            ConcurrentDictionary<string, Lazy<IConvention>>> _cache;

        protected ConventionCache(
            IServiceProvider services,
            ConcurrentDictionary<Type,
                ConcurrentDictionary<string, Lazy<IConvention>>> conventions)
        {
            _services = services;
            _cache = conventions;
        }

        public T GetOrAdd<T>(string name, Func<IServiceProvider, T> factory) where T : IConvention
        {
            ConcurrentDictionary<string, Lazy<IConvention>> conventionCache =
                _cache.GetOrAdd(typeof(T),
                    (t) => new ConcurrentDictionary<string, Lazy<IConvention>>());

            Lazy<IConvention> convention = conventionCache.GetOrAdd(name,
                (s) => new Lazy<IConvention>(() => factory(_services)));

            if (!(convention.Value is T conventionOfT))
            {

                if (convention.Value == null)
                {
                    throw new SchemaException(
                            SchemaErrorBuilder.New()
                            .SetMessage(
                                string.Format(
                                   TypeResources.ConventionCache_NonNull,
                                   typeof(T).Name,
                                   name))
                            .SetCode(ErrorCodes.Convention.NonNull)
                            .Build());
                }

                throw new SchemaException(
                            SchemaErrorBuilder.New()
                            .SetMessage(
                                string.Format(
                                   TypeResources.ConventionCache_WrongType,
                                   typeof(T).Name,
                                   name,
                                   convention.GetType().Name))
                            .SetCode(ErrorCodes.Convention.WrongType)
                            .Build());
            }

            return conventionOfT;
        }

        public static ConventionCache Create(
            IServiceProvider services,
            IEnumerable<ConfigureNamedConvention> configurations)
        {
            IServiceProvider serviceFactory = new ServiceFactory() { Services = services };
            var conventions
                = new ConcurrentDictionary<Type,
                    ConcurrentDictionary<string, Lazy<IConvention>>>();

            foreach ((var name, Type type, CreateConvention convention) in configurations)
            {
                ConcurrentDictionary<string, Lazy<IConvention>> conventionCache =
                    conventions.GetOrAdd(type,
                        (t) => new ConcurrentDictionary<string, Lazy<IConvention>>());

                conventionCache[name] =
                    new Lazy<IConvention>(() => convention.Invoke(serviceFactory));
            }

            return new ConventionCache(serviceFactory, conventions);
        }

    }
}
