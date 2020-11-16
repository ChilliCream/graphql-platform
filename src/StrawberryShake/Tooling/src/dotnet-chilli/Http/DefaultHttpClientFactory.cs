using System;
using System.Net.Http;
using System.Net.Http.Headers;
using IHttpClientFactory = StrawberryShake.Tools.Abstractions.IHttpClientFactory;

namespace StrawberryShake.Tools.Http
{
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        public HttpClient Create(Uri uri, string? token, string? scheme)
        {
            var httpClient = new HttpClient { BaseAddress = uri };
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    new ProductHeaderValue(
                        "StrawberryShake",
                        typeof(Program).Assembly!.GetName()!.Version!.ToString())));

            if (!(token is { }))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(scheme ?? "bearer", token);
            }

            return httpClient;
        }
    }
}
