using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

namespace StrawberryShake.Http
{
    public class HttpOperationContext
        : IHttpOperationContext
    {
        private Dictionary<string, object>? _contextData;

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
        }

        public IOperation Operation { get; }

        public IOperationResultBuilder Result { get; }

        public HttpRequestMessage? HttpRequest { get; set; }

        public HttpResponseMessage? HttpResponse { get; set; }

        public IDictionary<string, object> ContextData
        {
            get
            {
                if (_contextData is null)
                {
                    _contextData = new Dictionary<string, object>();
                }
                return _contextData;
            }
        }

        public HttpClient Client { get; }

        public IServiceProvider Services { get; }

        public CancellationToken RequestAborted { get; }
    }
}
