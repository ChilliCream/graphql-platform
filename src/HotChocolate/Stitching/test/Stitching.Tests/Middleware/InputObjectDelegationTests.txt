using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;
using HotChocolate.AspNetCore;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Moq;
using Microsoft.AspNetCore.TestHost;
using Snapshooter;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.Stitching
{
    public class InputObjectDelegationTests
        : StitchingTestBase
    {
        public InputObjectDelegationTests(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [Fact]
        public async Task AllowInputObjectTypesAsArguments()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            var connections = new Dictionary<string, HttpClient>();
            serviceCollection.AddSingleton(CreateRemoteSchemas(connections));

            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("server_1")
                .AddExtensionsFromString(
                    @"
                    extend type Query {
                        baz(a: Bar): String
                            @delegate(
                                schema: ""server_1""
                                path: ""foo(a:$arguments:a)"")
                    }
                    "));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = null;

            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                    .SetQuery("{ baz(a: { a: \"String 123\" }) }")
                    .SetServices(scope.ServiceProvider)
                    .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot(new SnapshotNameExtension("result"));
            executor.Schema.ToString().MatchSnapshot(
                new SnapshotNameExtension("schema"));
        }
        protected override IHttpClientFactory CreateRemoteSchemas(
               Dictionary<string, HttpClient> connections)
        {
            TestServer server_1 = TestServerFactory.Create(
                services => services.AddGraphQL(
                    SchemaBuilder.New()
                        .AddDocumentFromString
                        (
                            @"
                            type Query { foo(a: Bar): String }
                            input Bar { a: String }
                            "
                        )
                        .BindComplexType<Query1>(t => t.To("Query"))
                        .BindComplexType<Bar>()
                        .Create()),
                app => app.UseGraphQL());

            connections["server_1"] = server_1.CreateClient();

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(t => t.CreateClient(It.IsAny<string>()))
                .Returns(new Func<string, HttpClient>(n =>
                {
                    if (connections.ContainsKey(n))
                    {
                        return connections[n];
                    }

                    throw new Exception();
                }));
            return httpClientFactory.Object;
        }

        public class Query1
        {
            public string Foo(Bar a) => a.A;
        }

        public class Bar
        {
            public string A { get; set; }
        }
    }
}
