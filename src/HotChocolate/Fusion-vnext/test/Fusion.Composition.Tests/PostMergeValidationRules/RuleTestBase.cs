using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public abstract class RuleTestBase
{
    protected abstract object Rule { get; }
    private readonly CompositionLog _log = new();

    protected void AssertValid([StringSyntax("graphql")] string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var merger = new SourceSchemaMerger(
            schemas,
            new SourceSchemaMergerOptions { RemoveUnreferencedDefinitions = false });
        var mergeResult = merger.Merge();
        var validator = new PostMergeValidator(mergeResult.Value, [Rule], schemas, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(_log.IsEmpty);
    }

    protected void AssertInvalid([StringSyntax("graphql")] string[] sdl, string[] errorMessages)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var merger = new SourceSchemaMerger(
            schemas,
            new SourceSchemaMergerOptions { RemoveUnreferencedDefinitions = false });
        var mergeResult = merger.Merge();
        var validator = new PostMergeValidator(mergeResult.Value, [Rule], schemas, _log);

        // act
        validator.Validate();

        // assert
        _log.Select(e => e.ToString()).MatchInlineSnapshots(errorMessages);
    }
}
