using System.Net;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Shared;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

public class ErrorTests
{
    private readonly Func<ICompositionLog> _logFactory;

    public ErrorTests(ITestOutputHelper output)
    {
        _logFactory = () => new TestCompositionLog(output);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_Reformat_AuthorIds()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
                    },
                    FusionFeatureFlags.NodeField);

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Accounts.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query ReformatIds {
                reviews {
                    body
                    author {
                        birthdate
                    }
                }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();
    }

    private class ErrorFactory : IHttpClientFactory
    {
        private readonly IHttpClientFactory _innerFactory;
        private readonly string _errorClient;

        public ErrorFactory(IHttpClientFactory innerFactory, string errorClient)
        {
            _innerFactory = innerFactory;
            _errorClient = errorClient;
        }

        public HttpClient CreateClient(string name)
        {
            if (_errorClient.EqualsOrdinal(name))
            {
                var client = new HttpClient(new ErrorHandler());
                return client;
            }

            return _innerFactory.CreateClient(name);
        }

        private class ErrorHandler : HttpClientHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}
