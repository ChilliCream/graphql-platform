using System;
using System.Threading.Tasks;
using HotChocolate.Runtime;

namespace HotChocolate.Configuration
{
    public interface IDataLoaderConfiguration
    {
        void RegisterLoader<T>(
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, T> loaderFactory,
            Func<T, Task> triggerLoadAsync);
    }
}
