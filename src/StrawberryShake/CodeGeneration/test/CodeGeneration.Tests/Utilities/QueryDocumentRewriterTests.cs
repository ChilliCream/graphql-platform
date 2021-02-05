using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public class QueryDocumentRewriterTests
    {
        [Fact]
        public async Task GetReturnTypeName()
        {
            // arrange
            var schema =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWars()
                    .BuildSchemaAsync();

            schema =
                SchemaHelper.Load(
                    (string.Empty, schema.ToDocument()),
                    (string.Empty, Utf8GraphQLParser.Parse("extend schema @key(fields: \"id\")")));

            var document =
                Utf8GraphQLParser.Parse(@"
                    query GetHero {
                        hero(episode: NEW_HOPE) @returns(fragment: ""Hero"") {
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
                    }");

            // act
            document = QueryDocumentRewriter.Rewrite(document, schema);

            // assert
            document.Print().MatchSnapshot();
        }
    }
}
