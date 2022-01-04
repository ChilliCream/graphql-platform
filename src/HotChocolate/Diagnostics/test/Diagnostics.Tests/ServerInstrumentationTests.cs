using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Diagnostics;

public class ServerInstrumentationTests : ServerTestBase
{
    public ServerInstrumentationTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task SingleRequest_GetHeroName()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            TestServer server = CreateStarWarsServer(
                configureServices: services =>
                    services
                        .AddGraphQLServer()
                        .AddInstrumentation());

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero {
                            name
                        }
                    }"
                });

            // assert
            activities.MatchSnapshot();
        }
    }


}
