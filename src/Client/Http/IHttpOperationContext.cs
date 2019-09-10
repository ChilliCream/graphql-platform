using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

namespace StrawberryShake.Http
{
    public interface IHttpOperationContext
    {
        IOperation Operation { get; }
        IOperationResult Result { get; set; }
        HttpRequestMessage HttpRequest { get; set; }
        HttpResponseMessage HttpResponse { get; set; }
        IDictionary<string, object> ContextData { get; }
        HttpClient Client { get; }
        IServiceProvider Services { get; }
        CancellationToken RequestAborted { get; }
    }
}
