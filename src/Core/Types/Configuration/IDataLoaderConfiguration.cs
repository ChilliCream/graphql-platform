using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Runtime;

namespace HotChocolate.Configuration
{
    public interface IDataLoaderConfiguration
        : IFluent
    {
        void RegisterDataLoader(Type type,
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, object> loaderFactory = null,
            Func<object, CancellationToken, Task> triggerLoaderAsync = null);

        void RegisterDataLoader<T>(
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, T> loaderFactory = null,
            Func<T, CancellationToken, Task> triggerLoaderAsync = null);
    }
}
