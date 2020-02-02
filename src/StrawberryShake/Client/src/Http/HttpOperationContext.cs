using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using StrawberryShake.Transport;

namespace StrawberryShake.Http
{
    public class HttpOperationContext
        : IHttpOperationContext
        , IDisposable
    {
        private Dictionary<string, object>? _contextData;
        private bool _disposed;

        public HttpOperationContext(
            IOperation operation,
            IOperationFormatter operationFormatter,
            IOperationResultBuilder result,
            IResultParser resultParser,
            HttpClient client,
            CancellationToken requestAborted)
        {
            Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            OperationFormatter = operationFormatter
                ?? throw new ArgumentNullException(nameof(operationFormatter));
            Result = result
                ?? throw new ArgumentNullException(nameof(result));
            ResultParser = resultParser
                ?? throw new ArgumentNullException(nameof(resultParser));
            Client = client
                ?? throw new ArgumentNullException(nameof(client));
            RequestAborted = requestAborted;
            RequestWriter = new MessageWriter();
        }

        public IOperation Operation { get; }

        public IOperationFormatter OperationFormatter { get; }

        public IOperationResultBuilder Result { get; }

        public IResultParser ResultParser { get; }

        public IDictionary<string, object> ContextData
        {
            get
            {
                return _contextData ??= new Dictionary<string, object>();
            }
        }

        public CancellationToken RequestAborted { get; }

        public HttpRequestMessage? HttpRequest { get; set; }

        public HttpResponseMessage? HttpResponse { get; set; }

        public IRequestWriter RequestWriter { get; }

        public HttpClient Client { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                RequestWriter.Dispose();
                _disposed = true;
            }
        }
    }
}
