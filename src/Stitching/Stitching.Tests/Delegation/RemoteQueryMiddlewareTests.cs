using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Types;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Delegation
{
    public class RemoteQueryMiddlewareTests
        : IClassFixture<TestServerFactory>
    {
        public RemoteQueryMiddlewareTests(TestServerFactory testServerFactory)
        {
            TestServerFactory = testServerFactory;
        }

        private TestServerFactory TestServerFactory { get; set; }

        [Fact]
        public async Task ExecuteQueryOnRemoteSchema()
        {
            // arrange
            TestServer server = TestServerFactory.Create(
                ContractSchemaFactory.ConfigureSchema,
                ContractSchemaFactory.ConfigureServices,
                new QueryMiddlewareOptions());

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(t => t.CreateClient(It.IsAny<string>()))
                .Returns(server.CreateClient());

            var services = new ServiceCollection();
            services.AddSingleton(httpClientFactory.Object);

            ISchema schema = Schema.Create(
                FileResource.Open("Contract.graphql"),
                c =>
                {
                    c.RegisterType<DateTimeType>();
                    c.UseNullResolver();
                });

            IQueryExecutor executor = schema.MakeExecutable(b =>
                b.UseQueryDelegationPipeline("foo"));

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                new QueryRequest(@"{ contracts(customerId: ""Q3VzdG9tZXIteDE="") { id } }")
                {
                    Services = services.BuildServiceProvider()
                });

            // assert
            result.MatchSnapshot();
        }
    }
}
