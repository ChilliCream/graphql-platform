using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHeroWithFragmentIncludeAndSkipDirective;

public class StarWarsGetHeroWithFragmentIncludeAndSkipDirectiveTest : ServerTestBase
{
    public StarWarsGetHeroWithFragmentIncludeAndSkipDirectiveTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_StarWarsGetHeroWithFragmentIncludeAndSkipDirective_Test()
    {
        // arrange
        CancellationToken ct = new CancellationTokenSource(20_000).Token;
        using IWebHost host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            StarWarsGetHeroWithFragmentIncludeAndSkipDirectiveClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            StarWarsGetHeroWithFragmentIncludeAndSkipDirectiveClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddStarWarsGetHeroWithFragmentIncludeAndSkipDirectiveClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        StarWarsGetHeroWithFragmentIncludeAndSkipDirectiveClient client = services.GetRequiredService<StarWarsGetHeroWithFragmentIncludeAndSkipDirectiveClient>();

        // act
        var result = await client.GetHeroWithFragmentIncludeAndSkipDirective.ExecuteAsync(false, true, ct);

        // assert
        result.EnsureNoErrors();
        Assert.Null(result.Data!.Hero!.Friends!.IncludedPageInfo);
        Assert.Null(result.Data!.Hero!.Friends!.SkippedPageInfo);
    }
}
