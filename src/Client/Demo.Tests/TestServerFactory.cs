using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace StrawberryShake.Demo
{
    public class TestServerFactory
        : IDisposable
    {
        public List<TestServer> _instances = new List<TestServer>();

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
