using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Execution;

internal interface IRemoteBatchRequestHandler
{
    Task<IBatchQueryResult> ExecuteAsync(
        IEnumerable<IQueryRequest> requestBatch,
        CancellationToken cancellationToken = default);
}
