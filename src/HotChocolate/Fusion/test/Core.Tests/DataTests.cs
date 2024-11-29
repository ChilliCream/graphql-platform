using HotChocolate.Execution;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

public class DataTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Fetch_Data_Across_Subgraphs()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query GetUser {
              viewer {
                data {
                  accountValue
                  reviewsValue
                }
              }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }
}
