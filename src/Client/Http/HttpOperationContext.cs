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
            HttpClient client,
            IServiceProvider services,
            IOperationResultBuilder result,
            CancellationToken requestAborted)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Result = result;
            RequestAborted = requestAborted;
            MessageWriter = new MessageWriter();
        }

        public IOperation Operation { get; }

        public IOperationResultBuilder Result { get; }

        public HttpRequestMessage? HttpRequest { get; set; }

        public HttpResponseMessage? HttpResponse { get; set; }

        public IMessageWriter MessageWriter { get; }

        public IDictionary<string, object> ContextData
        {
            get
            {
                return _contextData ??= new Dictionary<string, object>();
            }
        }

        public HttpClient Client { get; }

        public IServiceProvider Services { get; }

        public CancellationToken RequestAborted { get; }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                MessageWriter.Dispose();
                _disposed = true;
            }
        }
    }
}
