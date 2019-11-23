using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using StrawberryShake.Transport;

namespace StrawberryShake.Http
{
    public interface IHttpOperationContext
    {
        IOperation Operation { get; }
        IOperationResultBuilder Result { get; }
        HttpRequestMessage? HttpRequest { get; set; }
        HttpResponseMessage? HttpResponse { get; set; }
        IMessageWriter MessageWriter { get; }
        IDictionary<string, object> ContextData { get; }
        HttpClient Client { get; }
        IServiceProvider Services { get; }
        CancellationToken RequestAborted { get; }
    }
}
