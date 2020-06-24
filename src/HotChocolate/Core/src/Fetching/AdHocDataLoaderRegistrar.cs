using System;
using System.Collections.Concurrent;
using GreenDonut;

namespace HotChocolate.Fetching
{
    /// <summary>
    /// Represents a registrar to register ad-hoc dataloader. An ad-hoc dataloader is created under
    /// the hood automatically and is based on predefined dataloader types. Therefore creating a
    /// specific dataloader type isn't required.
    /// </summary>
    public class AdHocDataLoaderRegistrar
        : IAdHocDataLoaderRegistrar
    {
        private readonly IBatchScheduler _batchScheduler;
        private readonly ConcurrentDictionary<string, IDataLoader> _instances =
            new ConcurrentDictionary<string, IDataLoader>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AdHocDataLoaderRegistrar" /> class.
        /// </summary>
        /// <param name="batchScheduler">A batch scheduler instance.</param>
        public AdHocDataLoaderRegistrar(IBatchScheduler batchScheduler)
        {
            _batchScheduler = batchScheduler ??
                throw new ArgumentNullException(nameof(batchScheduler));
        }

        /// <inheritdoc/>
        public TDataLoader GetOrAddDataLoader<TDataLoader>(
            string name,
            Func<IBatchScheduler, TDataLoader> factory)
                where TDataLoader : IDataLoader
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (_instances.TryGetValue(name, out IDataLoader cachedInstance))
            {
                if (cachedInstance is TDataLoader instance)
                {
                    return instance;
                }

                ThrowDifferentDataLoaderType();
            }

            TDataLoader newInstance = factory(_batchScheduler);

            if (!_instances.TryAdd(name, newInstance))
            {
                ThrowDifferentDataLoaderType();
            }

            return newInstance;
        }

        private static void ThrowDifferentDataLoaderType()
        {
            // todo: RST: ExecptionHelper
            throw new Exception("This name has been already registered to a data loader which is of a different type.");
        }
    }
}
