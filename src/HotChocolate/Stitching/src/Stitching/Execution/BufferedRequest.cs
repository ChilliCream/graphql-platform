using System;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Execution;

internal sealed class BufferedRequest
{
    public BufferedRequest(IQueryRequest request)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Promise = new TaskCompletionSource<IExecutionResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public IQueryRequest Request { get; }

    public TaskCompletionSource<IExecutionResult> Promise { get; }
}
