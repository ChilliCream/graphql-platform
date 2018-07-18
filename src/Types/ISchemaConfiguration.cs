using System;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    public interface ISchemaConfiguration
        : ISchemaFirstConfiguration
        , ICodeFirstConfiguration
        , IDataLoaderConfiguration
        , IUserStateConfiguration
    {
        ISchemaOptions Options { get; }

        void RegisterServiceProvider(IServiceProvider serviceProvider);
    }

    public interface IDataLoaderConfiguration
    {
        void RegisterLoader<T>(string key, ExecutionScope scope);
        void RegisterLoader<T>(string key, ExecutionScope scope, Func<T, Task> triggerLoadAsync);
    }

    public interface IUserStateConfiguration
    {

    }
}
