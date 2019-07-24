using System;
using System.Collections.Generic;
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
    public class Errorbehavior
        : StitchingTestBase
    {
        public Errorbehavior(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [Fact(Skip = "FIX THIS ONE ___ NULLREF ON WINDOWS BUILD SERVER")]
        public async Task ConnectionLost()
        {
            // arrange
            var connections = new Dictionary<string, HttpClient>();
            IHttpClientFactory clientFactory = CreateRemoteSchemas(connections);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .RenameType("CreateCustomerInput", "CreateCustomerInput2")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>())
                    .AddExecutionConfiguration(b =>
                    {
                        b.AddErrorFilter(error =>
                        {
                            if (error.Exception is Exception ex)
                            {
                                return ErrorBuilder.FromError(error)
                                    .ClearExtensions()
                                    .SetMessage(ex.GetType().FullName)
                                    .SetException(null)
                                    .Build();
                            };
                            return error;
                        });
                    }));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                            mutation {
                                createCustomer(input: { name: ""a"" })
                                {
                                    customer {
                                        name
                                        contracts {
                                            id
                                        }
                                    }
                                }
                            }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            var client = new HttpClient
            {
                BaseAddress = new Uri("http://127.0.0.1")
            }; ;
            connections["contract"] = client;
            connections["customer"] = client;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                            mutation {
                                createCustomer(input: { name: ""a"" })
                                {
                                    customer {
                                        name
                                        contracts {
                                            id
                                        }
                                    }
                                }
                            }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot();
        }

    }
}
