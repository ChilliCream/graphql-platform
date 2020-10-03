using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Client
{
    public class StitchingContextTests : StitchingTestBase
    {
        public StitchingContextTests(
           TestServerFactory testServerFactory)
           : base(testServerFactory)
        {
        }

        [Fact]
        public async Task String_Variable_Is_Converted_To_String_Literal()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(CreateRemoteSchemas());
            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("contract")
                .AddSchemaFromHttp("customer"));

            IServiceProvider services = serviceCollection.BuildServiceProvider();

            IStitchingContext stitchingContext = services.GetRequiredService<IStitchingContext>();
            IRemoteRequestExecutor customerRequestExecutor = stitchingContext.GetRemoteQueryClient("customer");

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("query ($id: ID!) { customer(id: $id) { name } }")
                .SetVariableValue("id", "Q3VzdG9tZXIKZDE=")
                .Create();

            Task<IExecutionResult> executeTask = customerRequestExecutor.ExecuteAsync(request);
            await customerRequestExecutor.DispatchAsync(CancellationToken.None);

            IExecutionResult result = await executeTask;

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Int_Variable_Is_Converted_To_Int_Literal()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(CreateFooServer());
            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromString("foo", "type Query { foo(a: Int!) : Int! }"));

            IServiceProvider services = serviceCollection.BuildServiceProvider();

            IStitchingContext stitchingContext = services.GetRequiredService<IStitchingContext>();
            IRemoteRequestExecutor customerRequestExecutor = stitchingContext.GetRemoteQueryClient("foo");

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("query ($foo: Int!) { foo(a: $foo) }")
                .SetVariableValue("foo", 1)
                .Create();

            Task<IExecutionResult> executeTask = customerRequestExecutor.ExecuteAsync(request);
            await customerRequestExecutor.DispatchAsync(CancellationToken.None);

            IExecutionResult result = await executeTask;

            result.MatchSnapshot();
        }

        private IHttpClientFactory CreateFooServer()
        {
            var connections = new Dictionary<string, HttpClient>();

            TestServer foo = TestServerFactory.Create(
                services => services.AddGraphQL(
                    SchemaBuilder.New()
                        .AddDocumentFromString("type Query { foo(a: Int!) : Int! }")
                        .AddResolver("Query", "foo", ctx => ctx.Argument<int>("a"))),
                app => app.UseGraphQL());

            connections["foo"] = foo.CreateClient();

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
    }
}
