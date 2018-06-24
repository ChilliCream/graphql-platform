using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.AspNetCore
{
    public class TestServerFactory
        : IDisposable
    {
        public List<TestServer> _instances = new List<TestServer>();

        public TestServer Create(Action<ISchemaConfiguration> configure, string route)
        {
            IWebHostBuilder builder = new WebHostBuilder()
                .Configure(app => app.UseGraphQL(route))
                .ConfigureServices(services =>
                {
                    services.AddGraphQL(configure);
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
