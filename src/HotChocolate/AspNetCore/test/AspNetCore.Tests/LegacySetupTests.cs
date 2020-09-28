using System;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore
{
    [Obsolete]
    public class LegacySetupTests : ServerTestBase
    {
        public LegacySetupTests(TestServerFactory serverFactory) : base(serverFactory)
        {
        }

        [Fact]
        public async Task AddGraphQL_With_UseGraphQL()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services
                    .AddGraphQL(
                        SchemaBuilder.New()
                            .AddQueryType(d => d
                                .Name("Query")
                                .Field("hello")
                                .Resolve("world"))
                            .Create()),
                app => app
                    .UseWebSockets()
                    .UseGraphQL());

            // act
            ClientQueryResult result = await server.PostAsync(
                new ClientQueryRequest { Query = "{ __typename }" });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AddGraphQL_With_UseGraphQL_With_Factory()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services
                    .AddGraphQL(sp =>
                        SchemaBuilder.New()
                            .AddServices(sp)
                            .AddQueryType(d => d
                                .Name("Query")
                                .Field("hello")
                                .Resolve("world"))
                            .Create()),
                app => app
                    .UseWebSockets()
                    .UseGraphQL());

            // act
            ClientQueryResult result = await server.PostAsync(
                new ClientQueryRequest { Query = "{ __typename }" });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AddGraphQL_With_UseGraphQL_With_SchemaBuilder()
        {
            // arrange
            TestServer server = ServerFactory.Create(
                services => services
                    .AddGraphQL(
                        SchemaBuilder.New()
                            .AddQueryType(d => d
                                .Name("Query")
                                .Field("hello")
                                .Resolve("world"))),
                app => app
                    .UseWebSockets()
                    .UseGraphQL());

            // act
            ClientQueryResult result = await server.PostAsync(
                new ClientQueryRequest { Query = "{ __typename }" });

            // assert
            result.MatchSnapshot();
        }
    }
}
