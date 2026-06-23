using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace StrawberryShake.CodeGeneration.Utilities;

public class QueryDocumentRewriterTests
{
    [Fact]
    public async Task GetReturnTypeName()
    {
        // arrange
        var schema = await TestSchemaHelper.CreateStarWarsSchemaAsync(
            "extend schema @key(fields: \"id\")");

        var document =
            Utf8GraphQLParser.Parse(
                """
                query GetHero {
                    hero(episode: NEW_HOPE) @returns(fragment: "Hero") {
                        ... Characters
                    }
                }

                fragment Characters on Character {
                    ... Human
                    ... Droid
                }

                fragment Hero on Character {
                    name
                }

                fragment Human on Human {
                    ... Hero
                    homePlanet
                }

                fragment Droid on Droid {
                    ... Hero
                    primaryFunction
                }
                """);

        // act
        document = QueryDocumentRewriter.Rewrite(document, schema);

        // assert
        document.Print().MatchSnapshot();
    }
}
