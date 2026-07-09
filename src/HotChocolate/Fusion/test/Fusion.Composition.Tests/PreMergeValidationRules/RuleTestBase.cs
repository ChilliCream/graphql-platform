using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

public abstract class RuleTestBase
{
    protected abstract object Rule { get; }
    private readonly CompositionLog _log = new();

    protected void AssertValid([StringSyntax("graphql")] string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new PreMergeValidator(schemas, [Rule], _log);

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
        var validator = new PreMergeValidator(schemas, [Rule], _log);

        // act
        validator.Validate();

        // assert
        _log.Select(e => e.ToString()).MatchInlineSnapshots(errorMessages);
    }
}
