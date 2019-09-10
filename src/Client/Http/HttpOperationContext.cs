using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

namespace StrawberryShake.Http
{
    public class HttpOperationContext
        : IHttpOperationContext
    {
        private Dictionary<string, object> _contextData;

        public HttpOperationContext(
            IOperation operation,
            HttpClient client,
            IServiceProvider services,
            CancellationToken requestAborted)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Services = services ?? throw new ArgumentNullException(nameof(services));
            RequestAborted = requestAborted;
        }

        public IOperation Operation { get; }

        public IOperationResult Result { get; set; }

        public HttpRequestMessage HttpRequest { get; set; }

        public HttpResponseMessage HttpResponse { get; set; }

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
