using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using StrawberryShake.Transport;

namespace StrawberryShake.Http
{
    public interface IHttpOperationContext
        : IOperationContext
    {
        HttpRequestMessage? HttpRequest { get; set; }
        HttpResponseMessage? HttpResponse { get; set; }
        IRequestWriter MessageWriter { get; }
        HttpClient Client { get; }
    }
}
