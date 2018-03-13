using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Prometheus.Execution;

namespace Prometheus.AspNet
{
    public class TestServerFactory
        : IDisposable
    {
        public List<TestServer> _instances = new List<TestServer>();

        public TestServer Create(ISchema schema, string route)
        {
            IWebHostBuilder builder = new WebHostBuilder()
                .Configure(app => app.UseGraphQL(route))
                .ConfigureServices(services =>
                {
                    services.AddScoped<ISchema>(s => schema)
                        .AddSingleton<IDocumentExecuter, DocumentExecuter>();
                });

            TestServer server = new TestServer(builder);
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