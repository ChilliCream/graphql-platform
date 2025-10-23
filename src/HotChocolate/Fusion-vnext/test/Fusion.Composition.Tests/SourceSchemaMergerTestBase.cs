using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion;

public abstract class SourceSchemaMergerTestBase
{
    protected static void AssertMatches(
        [StringSyntax("graphql")] string[] sdl,
        [StringSyntax("graphql")] string executionSchema,
        Action<SourceSchemaMergerOptions>? configure = null)
    {
        // arrange
        var options = new SourceSchemaMergerOptions
        {
            AddFusionDefinitions = false,
            RemoveUnreferencedTypes = false
        };
        configure?.Invoke(options);
        var merger = new SourceSchemaMerger(CreateSchemaDefinitions(sdl), options);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchInlineSnapshot(executionSchema);
    }
}
