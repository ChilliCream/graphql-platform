using HotChocolate.Language;

namespace HotChocolate.Types.Directives;

public class DeprecatedDirectiveTypeTests : TypeTestBase
{
    [Fact]
    public void ReasonArgument_Should_BeNonNullWithTheDefaultReason()
    {
        // arrange
        var schema = CreateSchema(_ => { });

        // act
        var reason = schema.DirectiveTypes["deprecated"].Arguments["reason"];

        // assert
        Assert.Equal("String!", reason.Type.Print());
        Assert.Equal("\"No longer supported\"", reason.DefaultValue?.ToString());
    }

    [Fact]
    public void ReasonArgument_Should_FailSchemaBuilding_When_AppliedWithNull()
    {
        // act
        static void Action()
            => SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Directive("deprecated", new ArgumentNode("reason", NullValueNode.Default))
                    .Resolve("bar"))
                .Create();

        // assert
        var exception = Assert.Throws<SchemaException>(Action);
        Assert.Single(exception.Errors).Message.MatchInlineSnapshot(
            """
            The directive arguments have invalid values: 'Cannot accept null for non-nullable input.' at reason.
            @deprecated(reason: null)
            """);
    }
}
