using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
    {
        private Dictionary<string, DataLoaderDescriptor> _dataLoaders =
            new Dictionary<string, DataLoaderDescriptor>();

        internal IReadOnlyCollection<DataLoaderDescriptor>
            DataLoaderDescriptors => _dataLoaders.Values;

        public void RegisterDataLoader<T>(
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, T> loaderFactory = null,
            Func<T, CancellationToken, Task> triggerLoaderAsync = null)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Func<IServiceProvider, object> factory = null;
            if (loaderFactory != null)
            {
                factory = new Func<IServiceProvider, object>(
                    sp => loaderFactory(sp));
            }

            TriggerDataLoaderAsync trigger = null;
            if (triggerLoaderAsync != null)
            {
                trigger = new TriggerDataLoaderAsync(
                    (o, c) => triggerLoaderAsync((T)o, c));
            }

            var descriptor = new DataLoaderDescriptor(
                key, typeof(T), scope,
                factory,
                trigger);
            _dataLoaders[key] = descriptor;
        }
    }
}
