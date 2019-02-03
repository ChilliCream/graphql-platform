using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Runtime;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
    {
        [Obsolete(
           "Use the IQueryContext.ContextData / IResolverContext.ContextData" +
           "instead. See https://hotchocolate.io/docs/migration " +
           "for more information." +
           "This type will be removed with version 1.0.0.",
           true)]
        public void RegisterDataLoader(Type type,
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, object> loaderFactory = null,
            Func<object, CancellationToken, Task> triggerLoaderAsync = null)
        {
            throw new NotSupportedException();
        }

        [Obsolete(
           "Use the IQueryContext.ContextData / IResolverContext.ContextData" +
           "instead. See https://hotchocolate.io/docs/migration " +
           "for more information." +
           "This type will be removed with version 1.0.0.",
           true)]
        public void RegisterDataLoader<T>(
            string key,
            ExecutionScope scope,
            Func<IServiceProvider, T> loaderFactory = null,
            Func<T, CancellationToken, Task> triggerLoaderAsync = null)
        {
            throw new NotSupportedException();
        }
    }
}
