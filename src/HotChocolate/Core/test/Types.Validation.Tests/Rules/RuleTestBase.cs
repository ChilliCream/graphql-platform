using System.Diagnostics.CodeAnalysis;
using HotChocolate.Logging;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Types.Validation.Rules;

public abstract class RuleTestBase<TRule> where TRule : new()
{
    private static readonly object s_rule = new TRule();
    private readonly ValidationLog _log = new();

    protected void AssertValid(
        [StringSyntax("graphql")] string sdl,
        Action<MutableSchemaDefinition>? configure = null)
    {
        // arrange
        var schema = SchemaParser.Parse(sdl);
        configure?.Invoke(schema);
        var validator = new SchemaValidator([s_rule]);

        // act
        var success = validator.Validate(schema, _log);

        // assert
        Assert.True(success);
        Assert.True(_log.IsEmpty);
    }

    protected void AssertInvalid(
        [StringSyntax("graphql")] string sdl,
        [StringSyntax(StringSyntaxAttribute.Json)] params string[] logEntries)
    {
        // arrange
        var schema = SchemaParser.Parse(sdl);
        var validator = new SchemaValidator([s_rule]);

        // act
        var success = validator.Validate(schema, _log);

        // assert
        Assert.False(success);
        _log.Select(e => e.ToString()).MatchInlineSnapshots(logEntries);
    }

    protected void AssertInvalid(
        ISchemaDefinition schema,
        [StringSyntax(StringSyntaxAttribute.Json)] params string[] logEntries)
    {
        // arrange
        var validator = new SchemaValidator([s_rule]);

        // act
        var success = validator.Validate(schema, _log);

        // assert
        Assert.False(success);
        _log.Select(e => e.ToString()).MatchInlineSnapshots(logEntries);
    }
}
