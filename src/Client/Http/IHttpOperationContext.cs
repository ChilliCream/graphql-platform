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
        IResultParser ResultParser { get; }
        IDictionary<string, object> ContextData { get; }
        CancellationToken RequestAborted { get; }
        HttpRequestMessage? HttpRequest { get; set; }
        HttpResponseMessage? HttpResponse { get; set; }
        IMessageWriter MessageWriter { get; }
        HttpClient Client { get; }
    }
}
