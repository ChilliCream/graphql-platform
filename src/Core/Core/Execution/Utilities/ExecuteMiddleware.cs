using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    public delegate Task<object> ExecuteMiddleware(
        IResolverContext context,
        Func<Task<object>> executeResolver);
}
