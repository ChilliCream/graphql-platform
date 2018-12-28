using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.DataLoader;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.DataLoader
{
    public delegate Fetch<TKey, TValue> FetchFactory<TKey, TValue>(
        IServiceProvider services);

    public delegate Task<IReadOnlyDictionary<TKey, TValue>> Fetch<TKey, TValue>(
        IReadOnlyCollection<TKey> keys);

    public delegate FetchGrouped<TKey, TValue> FetchGroupedFactory<TKey, TValue>(
        IServiceProvider services);

    public delegate Task<ILookup<TKey, TValue>> FetchGrouped<TKey, TValue>(
        IReadOnlyCollection<TKey> keys);

    public delegate FetchSingle<TKey, TValue> FetchSingleFactory<TKey, TValue>(
        IServiceProvider services);

    public delegate Task<TValue> FetchSingle<TKey, TValue>(TKey key);

    public delegate FetchOnce<TValue> FetchOnceFactory<TValue>(
        IServiceProvider services);

    public delegate Task<TValue> FetchOnce<TValue>();

    public interface IDataLoaderRegistry
    {
        bool Register<T>(
            string key,
            Func<IServiceProvider, T> factory);

        bool TryGet<T>(
            string key,
            out T dataLoader);
    }

    public static class DataLoaderRegistryExtensions
    {
        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchFactory<TKey, TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return registry.Register(key, services =>
                new FetchDataLoader<TKey, TValue>(
                    factory(services)));
        }

        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            Fetch<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return registry.Register(key, services =>
                new FetchDataLoader<TKey, TValue>(fetch));
        }

        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchGroupedFactory<TKey, TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return registry.Register(key, services =>
                new FetchGroupedDataLoader<TKey, TValue>(
                    factory(services)));
        }

        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchGrouped<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return registry.Register(key, services =>
                new FetchGroupedDataLoader<TKey, TValue>(fetch));
        }

        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchSingleFactory<TKey, TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return registry.Register(key, services =>
                new FetchSingle<TKey, TValue>(
                    factory(services)));
        }

        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchSingle<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return registry.Register(key, services =>
                new FetchSingle<TKey, TValue>(fetch));
        }

        public static bool Register<TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchOnceFactory<TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return registry.Register(key, services =>
            {
                FetchOnce<TValue> fetch = factory(services);
                return new FetchSingle<string, TValue>(k => fetch());
            });
        }

        public static bool Register<TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchOnce<TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return registry.Register(key, services =>
            {
                return new FetchSingle<string, TValue>(k => fetch());
            });
        }
    }

    public class DataLoaderRegistry
        : IDataLoaderRegistry
        , IBatchOperation
    {
        public int BatchSize => throw new NotImplementedException();

        public event EventHandler<EventArgs> BatchSizeIncreased;

        public Task InvokeAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        public bool Register<T>(string key, Func<IServiceProvider, T> factory)
        {
            throw new NotImplementedException();
        }

        public bool TryGet<T>(string key, out T dataLoader)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class FetchDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue>
    {
        private readonly Fetch<TKey, TValue> _fetch;

        public FetchDataLoader(Fetch<TKey, TValue> fetch)
            : base(new DataLoaderOptions<TKey> { AutoDispatching = false })
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override async Task<IReadOnlyList<IResult<TValue>>> Fetch(
            IReadOnlyList<TKey> keys)
        {
            IReadOnlyDictionary<TKey, TValue> result = await _fetch(keys);
            var items = new IResult<TValue>[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                if (result.TryGetValue(keys[i], out TValue value))
                {
                    items[i] = Result<TValue>.Resolve(value);
                }
            }

            return items;
        }
    }

    internal sealed class FetchGroupedDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue[]>
    {
        private readonly FetchGrouped<TKey, TValue> _fetch;

        public FetchGroupedDataLoader(FetchGrouped<TKey, TValue> fetch)
            : base(new DataLoaderOptions<TKey> { AutoDispatching = false })
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override async Task<IReadOnlyList<IResult<TValue[]>>> Fetch(
            IReadOnlyList<TKey> keys)
        {
            ILookup<TKey, TValue> result = await _fetch(keys);
            var items = new IResult<TValue[]>[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                items[i] = Result<TValue[]>.Resolve(result[keys[i]].ToArray());
            }

            return items;
        }
    }

    internal sealed class FetchSingleDataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue>
    {
        private readonly FetchSingle<TKey, TValue> _fetch;

        public FetchSingleDataLoader(FetchSingle<TKey, TValue> fetch)
            : base(new DataLoaderOptions<TKey> { AutoDispatching = false })
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        protected override async Task<IReadOnlyList<IResult<TValue>>> Fetch(
            IReadOnlyList<TKey> keys)
        {
            var items = new IResult<TValue>[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                try
                {
                    TValue value = await _fetch(keys[i]);
                    items[i] = Result<TValue>.Resolve(value);
                }
                catch (Exception ex)
                {
                    items[i] = Result<TValue>.Reject(ex);
                }
            }

            return items;
        }
    }
}

namespace HotChocolate.Resolvers
{

    public static class DataLoaderResolverContextExtensions
    {
        public static IDataLoader<TKey, TValue> DataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchFactory<TKey, TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (TryGetDataLoader(context, key,
                out IDataLoader<TKey, TValue> dataLoader,
                out IDataLoaderRegistry registry))
            {
                return dataLoader;
            }

            return GetOrCreate<IDataLoader<TKey, TValue>>(
                key, registry, r => r.Register(key, factory));
        }

        public static IDataLoader<TKey, TValue> DataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            Fetch<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return DataLoader(context, key, services => fetch);
        }

        public static IDataLoader<TKey, TValue[]> DataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchGroupedFactory<TKey, TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (TryGetDataLoader(context, key,
                out IDataLoader<TKey, TValue[]> dataLoader,
                out IDataLoaderRegistry registry))
            {
                return dataLoader;
            }

            return GetOrCreate<IDataLoader<TKey, TValue[]>>(
                key, registry, r => r.Register(key, factory));
        }

        public static IDataLoader<TKey, TValue[]> DataLoader<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchGrouped<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return DataLoader(context, key, services => fetch);
        }

        public static bool Register<TKey, TValue>(
            this IResolverContext context,
            string key,
            FetchSingleFactory<TKey, TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return registry.Register(key, services =>
                new FetchSingle<TKey, TValue>(
                    factory(services)));
        }

        public static bool Register<TKey, TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchSingle<TKey, TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return registry.Register(key, services =>
                new FetchSingle<TKey, TValue>(fetch));
        }

        public static bool Register<TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchOnceFactory<TValue> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return registry.Register(key, services =>
            {
                FetchOnce<TValue> fetch = factory(services);
                return new FetchSingle<string, TValue>(k => fetch());
            });
        }

        public static bool Register<TValue>(
            this IDataLoaderRegistry registry,
            string key,
            FetchOnce<TValue> fetch)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot be null or empty.",
                    nameof(key));
            }

            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            return registry.Register(key, services =>
            {
                return new FetchSingle<string, TValue>(k => fetch());
            });
        }

        public static bool TryGetDataLoader<T>(
            IResolverContext context,
            string key,
            out T dataLoader,
            out IDataLoaderRegistry registry)
        {
            registry = null;

            foreach (IDataLoaderRegistry current in
                context.Service<IEnumerable<IDataLoaderRegistry>>())
            {
                registry = current;

                if (current.TryGet(key, out dataLoader))
                {
                    return true;
                }
            }

            dataLoader = default(T);
            return false;
        }

        public static T GetOrCreate<T>(
            string key,
            IDataLoaderRegistry registry,
            Action<IDataLoaderRegistry> register)
        {
            if (registry == null)
            {
                // TODO : resources
                throw new InvalidOperationException(
                    "No DataLoader registry was registerd with your " +
                    "dependency injection.");
            }

            register(registry);

            if (!registry.TryGet(key, out T dataLoader))
            {
                // TODO : resources
                throw new InvalidOperationException(
                    "Unable to register a DataLoader with your " +
                    "DataLoader registry.");
            }

            return dataLoader;
        }
    }
}
