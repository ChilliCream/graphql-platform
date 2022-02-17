using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;
using ChilliCream.Testing;
using System.Net.Http;

namespace HotChocolate.Stitching.Integration;

public class PassThroughTests : IClassFixture<StitchingTestContext>
{
    public PassThroughTests(StitchingTestContext context)
    {
        Context = context;
    }

    private StitchingTestContext Context { get; }

    [Fact]
    public async Task AutoMerge_Schema()
    {
        // arrange
        const string coinService = "CoinService";
        var remoteSchemaSdl = FileResource.Open($"{coinService}.graphql");


        // act
        ISchema schema =
            await new ServiceCollection()
                .AddSingleton(CreateDummyFactory())
                .AddGraphQL()
                .AddRemoteSchemaFromString(coinService, remoteSchemaSdl)
                .BuildSchemaAsync();

        // assert
        schema.Print().MatchSnapshot();

        static IHttpClientFactory CreateDummyFactory()
        {
            var connections = new Dictionary<string, HttpClient>
            {
                { coinService, new HttpClient() }
            };
            return StitchingTestContext.CreateRemoteSchemas(connections);
        }
    }
}
