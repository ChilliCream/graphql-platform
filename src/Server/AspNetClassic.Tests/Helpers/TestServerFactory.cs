using System;
using System.Collections.Generic;
using System.Web.Http;
using HotChocolate.AspNetClassic.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Testing;
using Owin;

namespace HotChocolate.AspNetClassic
{
    public class TestServerFactory
        : IDisposable
    {
        public readonly List<TestServer> _instances = new List<TestServer>();

        public TestServer Create(
            Action<IServiceCollection> configureServices,
            Action<IAppBuilder, IServiceProvider> configureApplication)
        {
            var server = TestServer.Create(appBuilder =>
            {
                var httpConfig = new HttpConfiguration();
                var serviceCollection = new ServiceCollection();

                serviceCollection.AddOwinContextAccessor();
                configureServices?.Invoke(serviceCollection);

                IServiceProvider serviceProvider = serviceCollection
                    .BuildServiceProvider();

                httpConfig.DependencyResolver =
                    new DefaultDependencyResolver(serviceProvider);
                configureApplication(appBuilder, serviceProvider);
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
