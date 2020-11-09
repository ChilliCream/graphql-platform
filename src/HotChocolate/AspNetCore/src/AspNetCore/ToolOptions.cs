using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public class ToolOptions
    {
        public string? Document { get; set; }

        public DefaultCredentials? Credentials { get; set; }

        public IHeaderDictionary? HttpHeaders { get; set; }

        public DefaultHttpMethod? HttpMethod { get; set; }
    }

    public enum DefaultCredentials
    {
        Include,
        Omit,
        SameOrigin,
    }

    public enum DefaultHttpMethod
    {
        Get,
        Post
    }
}
