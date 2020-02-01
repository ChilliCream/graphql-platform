using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Snapshooter.Xunit;
using HotChocolate.Execution;
using HotChocolate.AspNetCore.Tests.Utilities;
using Snapshooter;
using HotChocolate.Types;
using HotChocolate.Language;
using System.Collections.Generic;
using Microsoft.AspNetCore.TestHost;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using HotChocolate.AspNetCore;
using HotChocolate.Stitching.Schemas.SpecialCases;
using Moq;

namespace HotChocolate.Stitching
{
    public class ScalarTests
        : StitchingTestBase
    {
        public ScalarTests(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [InlineData("date_field")]
        [InlineData("date_time_field")]
        [InlineData("string_field")]
        [InlineData("id_field")]
        [InlineData("byte_field")]
        [InlineData("int_field")]
        [InlineData("long_field")]
        [InlineData("float_field")]
        [InlineData("decimal_field")]
        [Theory]
        public async Task Scalar_Serializes_And_Deserializes_Correctly(
        string fieldName)
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("contract")
                .AddSchemaFromHttp("customer"));

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
                        .SetQuery($@"
                            query a($contractId: ID!) {{
                                contract(contractId: $contractId) {{
                                    ... on LifeInsuranceContract {{
                                        {fieldName}
                                    }}
                                }}
                            }}")
                        .SetVariableValue(
                            "contractId",
                            "TGlmZUluc3VyYW5jZUNvbnRyYWN0CmQx")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot(new SnapshotNameExtension(fieldName));
        }

        [Fact]
        public async Task Custom_Scalar_Types()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("special")
                .AddSchemaConfiguration(c =>
                    c.RegisterType<MyCustomScalarType>()));

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
                        .SetQuery("{ custom_scalar(bar: \"custom_string\") }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Custom_Scalar_Delegated_Argument()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("special")
                .AddExtensionsFromString("extend type Query { custom_scalar_stitched(bar: MyCustomScalarValue): MyCustomScalarValue @delegate(schema: \"special\", path: \"custom_scalar(bar: $arguments:bar)\") }")
                .AddSchemaConfiguration(c => {
                    c.RegisterType<MyCustomScalarType>();
                }));

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
                        .SetQuery("{ custom_scalar_stitched(bar: \"2019-11-11\") }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Custom_Scalar_Delegated_Input_Argument()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("special")
                .AddExtensionsFromString("extend type Query { custom_scalar_complex_stitched(bar: CustomInputValueInput): MyCustomScalarValue @delegate(schema: \"special\", path: \"custom_scalar_complex(bar: $arguments:bar)\") }")
                .AddSchemaConfiguration(c => {
                    c.RegisterType<MyCustomScalarType>();
                }));

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
                        .SetQuery("query ($bar: CustomInputValueInput) { custom_scalar_complex_stitched(bar: $bar) }")
                        .SetVariableValue("bar", new Dictionary<string, object> { { "from", "2019-11-11" }, { "to", "2019-11-17" } })
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Custom_Scalar_Delegated_Input_Argument_Unstitched()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("special")
                .AddSchemaConfiguration(c => {
                    c.RegisterType<MyCustomScalarType>();
                }));

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
                        .SetQuery("query ($bar: CustomInputValueInput) { custom_scalar_complex(bar: $bar) }")
                        .SetVariableValue("bar", new Dictionary<string, object> { { "from", "2019-11-11" }, { "to", "2019-11-17" } })
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot();
        }

        protected override IHttpClientFactory CreateRemoteSchemas(
            Dictionary<string, HttpClient> connections)
        {
            TestServer server_contracts = TestServerFactory.Create(
                ContractSchemaFactory.ConfigureServices,
                app => app.UseGraphQL());

            TestServer server_customers = TestServerFactory.Create(
                CustomerSchemaFactory.ConfigureServices,
                app => app.UseGraphQL());

            TestServer server_special = TestServerFactory.Create(
                SpecialCasesSchemaFactory.ConfigureServices,
                app => app.UseGraphQL());

            connections["contract"] = server_contracts.CreateClient();
            connections["customer"] = server_customers.CreateClient();
            connections["special"] = server_special.CreateClient();

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
