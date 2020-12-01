using System;
using System.Net.Http;

namespace StrawberryShake.Tools
{
    public interface IHttpClientFactory
    {
        HttpClient Create(Uri uri, string? token, string? scheme);
    }
}
