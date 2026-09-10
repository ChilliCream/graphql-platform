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
        AssertValid(sdl, Rule);
    }

    protected void AssertValid([StringSyntax("graphql")] string[] sdl, object rule)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new PreMergeValidator(schemas, [rule], _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(_log.IsEmpty);
    }

    protected void AssertInvalid([StringSyntax("graphql")] string[] sdl, string[] errorMessages)
    {
        AssertInvalid(sdl, errorMessages, Rule);
    }

    protected void AssertInvalid(
        [StringSyntax("graphql")] string[] sdl,
        string[] errorMessages,
        object rule)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new PreMergeValidator(schemas, [rule], _log);

        // act
        validator.Validate();

        // assert
        _log.Select(e => e.ToString()).MatchInlineSnapshots(errorMessages);
    }
}
