using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public abstract class CompositionTestBase(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

     protected async Task Succeed(string schema, string[]? extensions = null)
    {
        // arrange
        var configA = new SubgraphConfiguration(
            "A",
            schema,
            extensions ?? [],
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5001/graphql")), },
            null);

        // act
        var composer = new FusionGraphComposer(logFactory: _logFactory);
        var fusionConfig = await composer.ComposeAsync(new[] { configA, });

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }

    protected async Task Succeed(string schema, string schemaB)
    {
        // arrange
        var configA = new SubgraphConfiguration(
            "A",
            schema,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5001/graphql")), },
            null);

        var configB = new SubgraphConfiguration(
            "B",
            schemaB,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5002/graphql")), },
            null);

        // act
        var composer = new FusionGraphComposer(logFactory: _logFactory);
        var fusionConfig = await composer.ComposeAsync(new[] { configA, configB, });

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }

    protected async Task Fail(string schemaA, string schemaB)
    {
        // arrange
        var configA = new SubgraphConfiguration(
            "A",
            schemaA,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5001/graphql")), },
            null);

        var configB = new SubgraphConfiguration(
            "B",
            schemaB,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5002/graphql")), },
            null);

        // act
        var log = new ErrorCompositionLog();
        var composer = new FusionGraphComposer(logFactory: () => log);
        await composer.TryComposeAsync(new[] { configA, configB, });

        var snapshot = new Snapshot();

        foreach (var error in log.Errors)
        {
            snapshot.Add(error.Message);
        }

        await snapshot.MatchAsync();
    }
}
