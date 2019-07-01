using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

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
            serviceCollection.AddSingleton<IHttpQueryRequestInterceptor>(
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
                            customer(id: ""Q3VzdG9tZXIteDE="") {
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
            : IHttpQueryRequestInterceptor
        {
            public Task OnResponseReceivedAsync(
                IHttpQueryRequest request,
                HttpResponseMessage response,
                IQueryResult result)
            {
                result.ContextData["foo"] = "bar";
                return Task.CompletedTask;
            }
        }
    }
}
