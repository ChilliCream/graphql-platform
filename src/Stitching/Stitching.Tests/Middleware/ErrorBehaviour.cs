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
    public class ErrorBehaviour
        : StitchingTestBase
    {
        public ErrorBehaviour(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

         [Fact]
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
                var request = new QueryRequest(@"
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
                    }");
                request.Services = scope.ServiceProvider;

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
                var request = new QueryRequest(@"
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
                    }");
                request.Services = scope.ServiceProvider;

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot();
        }

    }
}
