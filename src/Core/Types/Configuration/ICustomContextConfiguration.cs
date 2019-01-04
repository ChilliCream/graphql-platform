using System;
using HotChocolate.Runtime;

namespace HotChocolate
{
    public interface ICustomContextConfiguration
        : IFluent
    {
        [Obsolete(
           "Use the IQueryContext.ContextData / IResolverContext.ContextData" +
           "instead. See https://hotchocolate.io/docs/migrate_dataloader " +
           "for more information." +
           "This type will be removed with version 1.0.0.",
           true)]
        void RegisterCustomContext<T>(
            ExecutionScope scope,
            Func<IServiceProvider, T> contextFactory = null);
    }
}
