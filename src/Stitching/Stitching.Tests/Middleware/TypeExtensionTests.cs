using System;
using System.Net.Http;
using Xunit;
using Snapshooter.Xunit;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Types;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using System.Collections.Generic;

namespace HotChocolate.Stitching
{
    public class TypeExtensionTests
        : StitchingTestBase
    {
        public TypeExtensionTests(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [Fact]
        public async Task AddObjectTypeExtension()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddSchemaConfiguration(c => c
                        .RegisterType<PaginationAmountType>()
                        .RegisterType(new ObjectTypeExtension(d => d
                            .Name("Query")
                            .Field("foo")
                            .Type<StringType>()
                            .Resolver("bar")))));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                result = await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery("{ foo }")
                        .SetServices(scope.ServiceProvider)
                        .Create());
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task AddComplexMutationTypeExtension()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddSchemaConfiguration(c => c
                        .RegisterType<PaginationAmountType>()
                        .RegisterType(new ObjectTypeExtension(d => d
                            .Name("Mutation")
                            .Field("foo")
                            .Type<StringType>()
                            .Argument("bar", a => a.Type<NonNullType<ComplexInputType>>())
                            .Resolver(c => c.Argument<ComplexInput>("bar").Value.ToString())))));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                result = await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery("mutation($input: ComplexInput!) { foo(bar: $input) }")
                        .SetVariableValues(new Dictionary<string, object>
                        {
                            { "input", new Dictionary<string, object> { { "value", "EXCEL" } } }
                        })
                        .SetServices(scope.ServiceProvider)
                        .Create());
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task UseSchemaBuilder()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddSchemaConfiguration(c => c
                        .RegisterType<PaginationAmountType>()
                        .Extend().OnBeforeBuild(b => b.AddType(
                            new ObjectTypeExtension(d => d
                                .Name("Query")
                                .Field("foo")
                                .Type<StringType>()
                                .Resolver("bar"))))));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                result = await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery("{ foo }")
                        .SetServices(scope.ServiceProvider)
                        .Create());
            }

            // assert
            Snapshot.Match(result);
        }
    }

    public class ComplexInput
    {
        public FileFormat Value { get; set; }
    }

    public class ComplexInputType : InputObjectType<ComplexInput>
    {
        protected override void Configure(IInputObjectTypeDescriptor<ComplexInput> descriptor)
        {
            descriptor.Field("value")
                .Type<NonNullType<EnumType<FileFormat>>>();
        }
    }

    public enum FileFormat
    {
        Unknown,
        Excel
    }
}
