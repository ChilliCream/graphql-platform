using System;
using System.Net.Http;
using Xunit;
using HotChocolate.Execution;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.Stitching
{
    public class HttpInterceptorTests
        : StitchingTestBase
    {
        public HttpInterceptorTests(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [Fact]
        public async Task InterceptHttpRequestAndDelegateHeaders()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IHttpStitchingRequestInterceptor>(
                new DummyInterceptor());
            serviceCollection.AddSingleton(CreateRemoteSchemas());
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>())
                    .AddSchemaConfiguration(c =>
                        c.RegisterType(new ObjectTypeExtension(d => d
                            .Name("Customer")
                            .Field("inter")
                            .Type<StringType>()
                            .Directive<ComputedDirective>()
                            .Resolver(ctx =>
                            {
                                return ctx.ScopedContextData["foo"];
                            })))));


            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();

            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                        {
                            customer(id: ""Q3VzdG9tZXIKZDE="") {
                                inter
                            }
                        }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot();
        }

        public class DummyInterceptor
            : IHttpStitchingRequestInterceptor
        {
            public Task<IReadOnlyQueryResult> OnResponseReceivedAsync(
                IReadOnlyQueryRequest request,
                HttpResponseMessage response,
                IReadOnlyQueryResult result)
            {
                return Task.FromResult(
                    QueryResultBuilder.FromResult(result)
                        .SetContextData("foo", "bar")
                        .Create());
            }
        }
    }
}
