using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

#nullable enable

namespace HotChocolate.Stitching.Execution;

internal interface IRemoteRequestHandler
{
    bool CanHandle(IQueryRequest request);

    Task<IExecutionResult> ExecuteAsync(
        IQueryRequest request,
        CancellationToken cancellationToken = default);
}
