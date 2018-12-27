using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    internal delegate Task<object> ExecuteMiddleware(
        IResolverContext context,
        Func<Task<object>> executeResolver);
}
