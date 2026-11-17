using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace StrawberryShake.CodeGeneration.Utilities;

public class TypeNameQueryRewriterTests
{
    [Fact]
    public void Rewrite_Should_AddTypeName_When_OperationSelectionSetIsEmpty()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse("query Ping {}");

        // act
        var rewritten = TypeNameQueryRewriter.Rewrite(document);

        // assert
        Assert.Equal("query Ping { __typename }", rewritten!.Print(indented: false));
    }

    [Fact]
    public void Rewrite_Should_AddTypeName_When_InlineFragmentSelectionSetIsEmpty()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse("query GetHero { hero { ... on Droid {} } }");

        // act
        var rewritten = TypeNameQueryRewriter.Rewrite(document);

        // assert
        Assert.Equal(
            "query GetHero { hero { __typename ... on Droid { __typename } } }",
            rewritten!.Print(indented: false));
    }

    [Fact]
    public void Rewrite_Should_AddTypeName_When_FragmentDefinitionSelectionSetIsEmpty()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            "query GetHero { hero { ...EmptyCharacter } } fragment EmptyCharacter on Character {}");

        // act
        var rewritten = TypeNameQueryRewriter.Rewrite(document);

        // assert
        Assert.Equal(
            "query GetHero { hero { __typename ...EmptyCharacter } } "
            + "fragment EmptyCharacter on Character { __typename }",
            rewritten!.Print(indented: false));
    }
}
