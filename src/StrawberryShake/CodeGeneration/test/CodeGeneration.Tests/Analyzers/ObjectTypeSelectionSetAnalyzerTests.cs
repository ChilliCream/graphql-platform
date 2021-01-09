using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using HotChocolate.Execution;
using HotChocolate.Language;
using System.Linq;
using HotChocolate;
using Xunit;
using HotChocolate.Types;

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

            var character = schema.GetType<InterfaceType>("Character");

            var document =
                Utf8GraphQLParser.Parse(@"
                    {
                        hero(episode: NEW_HOPE) {
                            name
                        }
                    }");

            var operation = document
                .Definitions
                .OfType<OperationDefinitionNode>()
                .First();

            SelectionSetVariants selectionSetVariants =
                new FieldCollector(schema, document)
                    .CollectFields(operation.SelectionSet, schema.QueryType, Path.New("OP"));

            FieldSelection fieldSelection = selectionSetVariants.ReturnType.Fields.First();

            selectionSetVariants =
                new FieldCollector(schema, document)
                    .CollectFields(fieldSelection.SyntaxNode.SelectionSet!, character, Path.Root);

            // act
            var analyzer = new ObjectTypeSelectionSetAnalyzer();
            analyzer.Analyze(null, fieldSelection, selectionSetVariants);

            // assert
            Assert.Collection(
                selectionSetVariants.ReturnType.Fields,
                field => Assert.Equal("hero", field.ResponseName));
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
            analyzer.Analyze(context, fieldSelection, selectionSetVariants);

            // assert
            Assert.Collection(
                selectionSetVariants.ReturnType.Fields,
                field => Assert.Equal("hero", field.ResponseName));
        }
    }
}