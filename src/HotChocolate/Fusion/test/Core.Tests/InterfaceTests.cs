using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Language.Utf8GraphQLParser;
using static HotChocolate.Fusion.TestHelper;


namespace HotChocolate.Fusion;

public class InterfaceTests
{
    private readonly Func<ICompositionLog> _logFactory;

    public InterfaceTests(ITestOutputHelper output)
    {
        _logFactory = () => new TestCompositionLog(output);
    }

    [Fact]
    public async Task Query_Interface_List()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query Appointments {
              appointments {
                nodes {
                  patient {
                    id
                  }
                }
              }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .Create()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Query_Interface_List_With_Fragment()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
                demoProject.Patient1.ToConfiguration(),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query Appointments {
              appointments {
                nodes {
                  patient {
                    id
                    ... on Patient1 {
                        name
                    }
                  }
                }
              }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .Create()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Query_Interface_List_With_Fragment_Fetch()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Appointment.ToConfiguration(),
                        demoProject.Patient1.ToConfiguration(),
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query Appointments {
              appointments {
                nodes {
                  patient {
                    id
                    ... on Patient1 {
                        name
                    }
                  }
                }
              }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .Create()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectQueryResult().Errors);
    }
}
