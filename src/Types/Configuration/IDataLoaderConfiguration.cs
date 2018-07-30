using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Runtime;

namespace HotChocolate.Configuration
{
    public interface IDataLoaderConfiguration
        : IFluent
    {
        void RegisterDataLoader<T>(
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, T> loaderFactory = null,
            Func<T, CancellationToken, Task> triggerLoaderAsync = null);
    }
}
