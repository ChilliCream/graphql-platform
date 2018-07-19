using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    public interface IDataLoaderConfiguration
    {
        void RegisterLoader<T>(
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, T> loaderFactory,
            Func<T, Task> triggerLoadAsync);

        void RegisterLoader<T>(
            string key,
            ExecutionScope scope,
            Func<T, Task> triggerLoadAsync);
    }
}
