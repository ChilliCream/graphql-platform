using System.Diagnostics.CodeAnalysis;
using CookieCrumble;
using HotChocolate.Fusion.Composition.Pipeline;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public abstract class CompositionTestBase
{
    private readonly Func<ICompositionLog> _logFactory;
    private readonly ITypeMergeHandler[] _typeMergeHandler =
    {
        new ObjectTypeMergeHandler(),
        new InterfaceTypeMergeHandler(),
        new UnionTypeMergeHandler(),
        new InputObjectTypeMergeHandler(),
        new EnumTypeMergeHandler(),
        new ScalarTypeMergeHandler()
    };

    internal CompositionTestBase(ITestOutputHelper output, params ITypeMergeHandler[] typeMergeHandler)
    {
        _logFactory = () => new TestCompositionLog(output);

        if (typeMergeHandler.Length > 0)
        {
            _typeMergeHandler = typeMergeHandler;
        }
    }

     protected async Task<string> Succeed(string schema, string[]? extensions = null)
    {
        // arrange
        var configA = new SubgraphConfiguration(
            "A",
            schema,
            extensions ?? Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5001/graphql")) },
            null);

        // act
        var composer = new FusionGraphComposer2(
            entityEnrichers: Array.Empty<IEntityEnricher>(),
            mergeHandlers: _typeMergeHandler,
            logFactory: _logFactory);
        var fusionConfig = await composer.ComposeAsync(new[] { configA });

        return SchemaFormatter.FormatAsString(fusionConfig);
    }

    protected async Task<string> Succeed(string schema, string schemaB)
    {
        // arrange
        var configA = new SubgraphConfiguration(
            "A",
            schema,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5001/graphql")) },
            null);

        var configB = new SubgraphConfiguration(
            "B",
            schemaB,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5002/graphql")) },
            null);

        // act
        var composer = new FusionGraphComposer2(
            entityEnrichers: Array.Empty<IEntityEnricher>(),
            mergeHandlers: _typeMergeHandler,
            logFactory: _logFactory);
        var fusionConfig = await composer.ComposeAsync(new[] { configA, configB });

        return SchemaFormatter.FormatAsString(fusionConfig);
    }

    protected async Task Fail(string schemaA, string schemaB, params string[] expectedErrorCodes)
    {
        // arrange
        var configA = new SubgraphConfiguration(
            "A",
            schemaA,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5001/graphql")) },
            null);

        var configB = new SubgraphConfiguration(
            "B",
            schemaB,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5002/graphql")) },
            null);

        // act
        var log = new ErrorCompositionLog();
        var composer = new FusionGraphComposer2(
            entityEnrichers: Array.Empty<IEntityEnricher>(),
            mergeHandlers: _typeMergeHandler,
            logFactory: () => log);
        await composer.TryComposeAsync(new[] { configA, configB });

        var expectedErrorCodesSet = new HashSet<string>(expectedErrorCodes);
        var snapshot = new Snapshot();
        var hasError = false;

        foreach (var error in log.Errors)
        {
            hasError = true;

            if (error.Code is not null)
            {
                expectedErrorCodesSet.Remove(error.Code);
            }

            snapshot.Add(error.Message);
        }

        Assert.True(hasError, "No error found!");
        Assert.True(
            expectedErrorCodesSet.Count is 0,
            $"The following error codes where not raised {string.Join(",", expectedErrorCodesSet)}");
        await snapshot.MatchAsync();
    }
}
