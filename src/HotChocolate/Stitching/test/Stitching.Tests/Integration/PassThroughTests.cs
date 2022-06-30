using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

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
            return StitchingTestContext.CreateHttpClientFactory(connections);
        }
    }
}
