using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using Xunit;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class ObjectTypeSelectionSetAnalyzerTests
    {
        [Fact]
        public async Task Object_With_Default_Names()
        {
            // arrange
            var schema =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWars()
                    .BuildSchemaAsync();

            var document =
                Utf8GraphQLParser.Parse(@"
                    query GetHero {
                        hero(episode: NEW_HOPE) {
                            name
                        }
                    }");

            var context = new DocumentAnalyzerContext(schema, document);
            SelectionSetVariants selectionSetVariants = context.CollectFields();
            FieldSelection fieldSelection = selectionSetVariants.ReturnType.Fields.First();
            selectionSetVariants = context.CollectFields(fieldSelection);

            // act
            var analyzer = new ObjectTypeSelectionSetAnalyzer();
            var result = analyzer.Analyze(context, fieldSelection, selectionSetVariants);

            // assert
            Assert.Equal("IGetHero_Hero", result.Name);

            Assert.Collection(
                context.GetImplementations(result),
                model => Assert.Equal("GetHero_Hero", model.Name));

            Assert.Collection(
                result.Fields,
                field => Assert.Equal("Name", field.Name));
        }

        [Fact]
        public async Task Object_With_Fragment_Definition()
        {
            // arrange
            var schema =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWars()
                    .BuildSchemaAsync();

            var document =
                Utf8GraphQLParser.Parse(@"
                    query GetHero {
                        hero(episode: NEW_HOPE) {
                            ... Hero
                        }
                    }

                    fragment Hero on Character {
                        name
                    }");

            var context = new DocumentAnalyzerContext(schema, document);
            SelectionSetVariants selectionSetVariants = context.CollectFields();
            FieldSelection fieldSelection = selectionSetVariants.ReturnType.Fields.First();
            selectionSetVariants = context.CollectFields(fieldSelection);

            // act
            var analyzer = new ObjectTypeSelectionSetAnalyzer();
            var result = analyzer.Analyze(context, fieldSelection, selectionSetVariants);

            // assert
            Assert.Equal("IHero", result.Name);

            Assert.Collection(
                context.GetImplementations(result),
                model => Assert.Equal("Hero", model.Name));

            Assert.Collection(
                result.Fields,
                field => Assert.Equal("Name", field.Name));
        }
    }
}
