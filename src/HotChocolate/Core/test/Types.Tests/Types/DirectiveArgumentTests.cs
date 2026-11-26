namespace HotChocolate.Types;

public class DirectiveArgumentTests
{
    [Fact]
    public void Coordinate_Should_NameTheDirective_When_ArgumentBelongsToDirective()
    {
        // arrange
        const string sdl =
            """
            type Query {
                field: String
            }

            directive @foo(arg: Int) on FIELD
            """;

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(sdl)
            .Use(next => next)
            .Create();

        // assert
        var argument = schema.DirectiveTypes["foo"].Arguments["arg"];
        Assert.Equal("@foo(arg:)", argument.Coordinate.ToString());
    }
}
