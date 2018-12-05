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

        public void RegisterDataLoader(Type type,
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, object> loaderFactory = null,
            Func<object, CancellationToken, Task> triggerLoaderAsync = null){

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Func<IServiceProvider, object> factory = null;
            if (loaderFactory != null)
            {
                factory = loaderFactory;
            }

            TriggerDataLoaderAsync trigger = null;
            if (triggerLoaderAsync != null)
            {
                trigger = (o, c) => triggerLoaderAsync(o, c);
            }

            var descriptor = new DataLoaderDescriptor(
                key, type, scope,
                factory,
                trigger);
            _dataLoaders[key] = descriptor;
        }
        public void RegisterDataLoader<T>(
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, T> loaderFactory = null,
            Func<T, CancellationToken, Task> triggerLoaderAsync = null)
        {
            
            Func<IServiceProvider,object> f = null;
            if(loaderFactory != null)
                f = s => loaderFactory(s);
            Func<object,CancellationToken,Task> g = null;
            if(triggerLoaderAsync != null)
                g = (a,b) => triggerLoaderAsync((T)a,b);

            RegisterDataLoader(typeof(T),key,scope,f,g);
           
        }
    }
}
