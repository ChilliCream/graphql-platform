using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Logging;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public abstract class RuleTestBase
{
    protected abstract object Rule { get; }
    private readonly CompositionLog _log = new();

    protected void AssertValid([StringSyntax("graphql")] string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new SourceSchemaValidator(schemas, [Rule], _log);

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
        foreach (var schema in schemas)
        {
            if (schema.Types.TryGetType("__Type", out MutableObjectTypeDefinition? type))
            {
                type.IsIntrospectionType = true;
            }
        }
        var validator = new SourceSchemaValidator(schemas, [Rule], _log);

        // act
        validator.Validate();

        // assert
        _log.Select(e => e.ToString()).MatchInlineSnapshots(errorMessages);
    }
}
