using System.Net;

namespace HotChocolate.AspNetCore.Utilities
{
    public class ClientRawResult
    {
        public string ContentType { get; set; }

        public HttpStatusCode StatusCode { get; set; }
        
        public string Content { get; set; }
    }
}
