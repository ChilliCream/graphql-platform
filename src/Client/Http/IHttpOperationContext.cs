using System.Net.Http;
using StrawberryShake.Transport;

namespace StrawberryShake.Http
{
    public interface IHttpOperationContext
        : IOperationContext
    {
        HttpRequestMessage? HttpRequest { get; set; }
        HttpResponseMessage? HttpResponse { get; set; }
        IRequestWriter RequestWriter { get; }
        HttpClient Client { get; }
    }
}
