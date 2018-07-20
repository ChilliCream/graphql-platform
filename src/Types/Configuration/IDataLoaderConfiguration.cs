using System;
using System.Threading.Tasks;

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
