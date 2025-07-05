using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpClient(
        this IServiceCollection services,
        string name,
        TestServer server)
    {
        services.TryAddSingleton<IHttpClientFactory, Factory>();
        return services.AddSingleton(new TestServerRegistration(name, server));
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
                return registration.Server.CreateClient();
            }

            throw new InvalidOperationException(
                $"No test server registered with the name: {name}");
        }
    }

    private record TestServerRegistration(string Name, TestServer Server);
}
