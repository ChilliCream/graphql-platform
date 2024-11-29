using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using HttpClientConfiguration = HotChocolate.Fusion.Metadata.HttpClientConfiguration;

namespace HotChocolate.Fusion;

public class ConfigurationRewriterTests
{
    private readonly Func<ICompositionLog> _logFactory;

    public ConfigurationRewriterTests(ITestOutputHelper output)
    {
        _logFactory = () => new TestCompositionLog(output);
    }

    [Fact]
    public async Task Rewrite_HttpClient_Configuration()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        var configuration = SchemaFormatter.FormatAsDocument(fusionGraph);

        // act
        var rewriter = new CustomRewriter();
        var rewritten = await rewriter.RewriteAsync(configuration);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(configuration, "Original:");
        snapshot.Add(rewritten, "Rewritten:");
        await snapshot.MatchMarkdownAsync();

        // this should not throw
        var reader = new FusionGraphConfigurationReader();
        var config = reader.Read(rewritten);
        Assert.Contains(config.HttpClients, t => t.EndpointUri == new Uri("http://client"));
    }

    private class CustomRewriter : ConfigurationRewriter
    {
        protected override ValueTask<HttpClientConfiguration> RewriteAsync(
            HttpClientConfiguration configuration,
            CancellationToken cancellationToken)
        {
            return base.RewriteAsync(
                configuration with { EndpointUri = new Uri("http://client"), },
                cancellationToken);
        }
    }
}
