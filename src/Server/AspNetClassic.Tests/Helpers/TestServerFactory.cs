using System;
using System.Collections.Generic;
using System.Web.Http;
using HotChocolate.AspNetClassic.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Testing;

namespace HotChocolate.AspNetClassic
{
    public class TestServerFactory
        : IDisposable
    {
        public readonly List<TestServer> _instances = new List<TestServer>();

        public TestServer Create(
            Action<ISchemaConfiguration> configure,
            QueryMiddlewareOptions options)
        {
            return Create(configure, null, options);
        }

        public TestServer Create(
            Action<ISchemaConfiguration> configure,
            Action<IServiceCollection> configureServices,
            QueryMiddlewareOptions options)
        {
            var server = TestServer.Create(appBuilder =>
            {
                var httpConfig = new HttpConfiguration();
                var serviceCollection = new ServiceCollection();

                configureServices?.Invoke(serviceCollection);
                serviceCollection.AddScoped<TestService>();
                serviceCollection.AddGraphQL(configure);

                IServiceProvider serviceProvider = serviceCollection
                    .BuildServiceProvider();

                httpConfig.DependencyResolver =
                    new DefaultDependencyResolver(serviceProvider);
                appBuilder.UseGraphQL(serviceProvider, options);
            });

            _instances.Add(server);

            return server;
        }

        public void Dispose()
        {
            foreach (TestServer testServer in _instances)
            {
                testServer.Dispose();
            }
        }
    }
}
