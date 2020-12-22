using System;
using System.Collections.Generic;
using System.Threading;

namespace StrawberryShake
{
    public interface IConnection<TBody> where TBody : class
    {
        IAsyncEnumerable<Response<TBody>> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default);
    }

    public class Response<TBody> where TBody : class
    {
        public Response(TBody? body, Exception? exception)
        {
            Body = body;
            Exception = exception;
        }

        public TBody? Body { get; }

        public Exception? Exception { get; }
    }
}
