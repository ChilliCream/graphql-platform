using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsEmptySelectionSets;

public class StarWarsEmptySelectionSetsTest : ServerTestBase
{
    public StarWarsEmptySelectionSetsTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task GetHero_Should_ReturnDroid_When_ServerRejectsEmptySelectionSets()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);
        await using var app = StartServerRejectingEmptySelectionSets(out var port);
        var client = CreateClient(port);

        // act
        var result = await client.GetHero.ExecuteAsync(cts.Token);

        // assert
        result.EnsureNoErrors();
        Assert.IsType<GetHero_Hero_Droid>(result.Data!.Hero);
    }

    [Fact]
    public async Task GetHeroWithEmptyFragments_Should_ReturnHero_When_ServerRejectsEmptySelectionSets()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);
        await using var app = StartServerRejectingEmptySelectionSets(out var port);
        var client = CreateClient(port);

        // act
        var result = await client.GetHeroWithEmptyFragments.ExecuteAsync(cts.Token);

        // assert
        result.EnsureNoErrors();
        var hero = Assert.IsType<GetHeroWithEmptyFragments_Hero_Droid>(result.Data!.Hero);
        Assert.Equal("R2-D2", hero.Name);
    }

    [Fact]
    public async Task Ping_Should_ReturnResult_When_ServerRejectsEmptySelectionSets()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);
        await using var app = StartServerRejectingEmptySelectionSets(out var port);
        var client = CreateClient(port);

        // act
        var result = await client.Ping.ExecuteAsync(cts.Token);

        // assert
        result.EnsureNoErrors();
        Assert.IsType<PingResult>(result.Data);
    }

    private static StarWarsEmptySelectionSetsClient CreateClient(int port)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddStarWarsEmptySelectionSetsClient()
            .ConfigureHttpClient(
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();

        return services.GetRequiredService<StarWarsEmptySelectionSetsClient>();
    }

    private static WebApplication StartServerRejectingEmptySelectionSets(out int port)
    {
#pragma warning disable CS0618 // The obsolete option makes the server reject empty selection sets.
        return TestServerHelper.StartApp(
            builder => builder.ModifyOptions(o => o.EnableEmptySelectionSets = false),
            out port);
#pragma warning restore CS0618
    }
}
