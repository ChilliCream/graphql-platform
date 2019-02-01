using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    public class RemoteQueryClient
        : IRemoteQueryClient
    {
        private IQueryExecutor _executor;

        public RemoteQueryClient(IQueryExecutor executor)
        {
            _executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
        }

        public Task<IExecutionResult> ExecuteAsync(
            IResolverContext context,
            QueryRequest request)
        {
            return _executor.ExecuteAsync(request, context.RequestAborted);
        }
    }
}
