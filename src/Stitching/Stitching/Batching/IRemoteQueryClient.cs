using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    public interface IRemoteQueryClient
    {
        Task<IExecutionResult> ExecuteAsync(
            IResolverContext context,
            IReadOnlyQueryRequest request);
    }
}
