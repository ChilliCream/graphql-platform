using System;
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
        public void RegisterCustomContext<T>(
           ExecutionScope scope,
           Func<IServiceProvider, T> contextFactory = null)
        {
            throw new NotSupportedException();
        }
    }
}
