using System;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.Demo
{
    public class IntegrationTestBase
    {
        protected static IServiceProvider CreateServices(
            string clientName,
            int port,
            Action<ServiceCollection> services)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(
                clientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port));
            serviceCollection.AddWebSocketClient(
                clientName,
                c => c.Uri = new Uri("ws://localhost:" + port));
            services(serviceCollection);
            return serviceCollection.BuildServiceProvider();
        }
    }
}
