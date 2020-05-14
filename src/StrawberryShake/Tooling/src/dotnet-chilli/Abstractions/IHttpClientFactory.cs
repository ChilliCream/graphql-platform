using System;
using System.Net.Http;

namespace StrawberryShake.Tools.Abstractions
{
    public interface IHttpClientFactory
    {
        HttpClient Create(Uri uri, string? token, string? scheme);
    }
}
