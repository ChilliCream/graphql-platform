using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
        : ISchemaConfiguration
    {
        private Dictionary<string, DataLoaderDescriptor> _dataLoaders =
            new Dictionary<string, DataLoaderDescriptor>();

        internal IReadOnlyCollection<DataLoaderDescriptor>
            DataLoaderDescriptors => _dataLoaders.Values;

        public void RegisterLoader<T>(
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, T> loaderFactory,
            Func<T, CancellationToken, Task> triggerLoaderAsync)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var descriptor = new DataLoaderDescriptor(
                key, typeof(T), scope,
                sp => loaderFactory(sp),
                (o, c) => triggerLoaderAsync((T)o, c));
            _dataLoaders[key] = descriptor;
        }
    }
}
