using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Utilities
{
    public class TestServerFactory : IDisposable
    {
        private readonly List<TestServer> _instances = new List<TestServer>();

        public TestServer Create(
            Action<IServiceCollection> configureServices,
            Action<IApplicationBuilder> configureApplication)
        {
            IWebHostBuilder builder = new WebHostBuilder()
                .Configure(configureApplication)
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    configureServices?.Invoke(services);
                });

            var server = new TestServer(builder);
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
