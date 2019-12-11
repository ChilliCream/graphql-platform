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
using HotChocolate.Language;

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
        public async Task AddObjectTypeExtensionWithResolver()
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
                            .Field<ObjectResolver>(r => r.GetStitchedCustomer())
                            .Type(new NamedTypeNode("Customer"))))));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            ISchema schema = services
                .GetRequiredService<ISchema>();

            Snapshot.Match(schema.ToString());
        }

        public class ObjectResolver
        {
            public IReadOnlyDictionary<string, object> GetStitchedCustomer() => new Dictionary<string, object> {
                { "Id", "id" },
                { "Name", "John Doe" },
                { "Street", "Evergreen Tc" }
            };
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
}
