using System.Net;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpClient(
        this IServiceCollection services,
        string name,
        TestServer server,
        bool isOffline = false)
    {
        services.TryAddSingleton<IHttpClientFactory, Factory>();
        return services.AddSingleton(new TestServerRegistration(name, server, isOffline));
    }

    private class Factory : IHttpClientFactory
    {
        private readonly Dictionary<string, TestServerRegistration> _registrations;

        public Factory(IEnumerable<TestServerRegistration> registrations)
        {
            _registrations = registrations.ToDictionary(r => r.Name, r => r);
        }

        public HttpClient CreateClient(string name)
        {
            if (_registrations.TryGetValue(name, out var registration))
            {
                var client = registration.IsOffline
                    ? new HttpClient(new ErrorHandler())
                    : registration.Server.CreateClient();

                client.DefaultRequestHeaders.AddGraphQLPreflight();

                return client;
            }

            throw new InvalidOperationException(
                $"No test server registered with the name: {name}");
        }

    private class ErrorHandler : HttpClientHandler
            {
                protected override Task<HttpResponseMessage> SendAsync(
                    HttpRequestMessage request,
                    CancellationToken cancellationToken)
                    => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
    }

    private record TestServerRegistration(string Name, TestServer Server, bool IsOffline = false);
}
