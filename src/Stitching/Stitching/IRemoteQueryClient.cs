using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public delegate void RequestBufferedEventHandler(
        IRemoteQueryClient sender,
        EventArgs eventArgs);

    public interface IRemoteQueryClient
    {
        event RequestBufferedEventHandler BufferedRequest;


        int BufferSize { get; }

        Task<IExecutionResult> ExecuteAsync(
            IReadOnlyQueryRequest request);

        Task DispatchAsync(CancellationToken cancellationToken);
    }
}
