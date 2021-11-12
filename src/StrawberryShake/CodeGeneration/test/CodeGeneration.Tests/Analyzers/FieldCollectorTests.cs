using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class FieldCollectorTests
{
    [Fact]
    public async Task Collect_First_Level_No_Fragments()
    {
        // arrange
        ISchema? schema =
            await new ServiceCollection()
                .AddStarWarsRepositories()
                .AddGraphQL()
                .AddStarWars()
                .BuildSchemaAsync();

        DocumentNode? document =
            Utf8GraphQLParser.Parse(@"
                    {
                        hero(episode: NEW_HOPE) {
                            name
                            ... on Droid {
                                primaryFunction
                            }
                        }
                    }");

        OperationDefinitionNode? operation = document
            .Definitions
            .OfType<OperationDefinitionNode>()
            .First();

        // act
        SelectionSetVariants selectionSetVariants =
            new FieldCollector(schema, document)
                .CollectFields(operation.SelectionSet, schema.QueryType, Path.Root);

        // assert
        Assert.Collection(
            selectionSetVariants.ReturnType.Fields,
            field => Assert.Equal("hero", field.ResponseName));
    }

    [Fact]
    public async Task Collect_Second_Level_Fragments()
    {
        // arrange
        ISchema? schema =
            await new ServiceCollection()
                .AddStarWarsRepositories()
                .AddGraphQL()
                .AddStarWars()
                .BuildSchemaAsync();

        InterfaceType? character = schema.GetType<InterfaceType>("Character");

        DocumentNode? document =
            Utf8GraphQLParser.Parse(@"
                    {
                        hero(episode: NEW_HOPE) {
                            name
                            ... on Droid {
                                primaryFunction
                            }
                        }
                    }");

        OperationDefinitionNode? operation = document
            .Definitions
            .OfType<OperationDefinitionNode>()
            .First();

        FieldNode? secondLevel = operation
            .SelectionSet
            .Selections
            .OfType<FieldNode>()
            .First();

        // act
        SelectionSetVariants selectionSetVariants =
            new FieldCollector(schema, document)
                .CollectFields(secondLevel.SelectionSet!, character, Path.Root.Append("hero"));

        // assert
        Assert.Collection(
            selectionSetVariants.ReturnType.Fields,
            field => Assert.Equal("name", field.ResponseName));
        Assert.Equal("Character", selectionSetVariants.ReturnType.Type.Name.Value);
        Assert.Equal("Human", selectionSetVariants.Variants[0].Type.Name.Value);
        Assert.Equal("Droid", selectionSetVariants.Variants[1].Type.Name.Value);

        Assert.Collection(
            selectionSetVariants.Variants[1].FragmentNodes,
            fragmentNode => Assert.Equal(FragmentKind.Inline, fragmentNode.Fragment.Kind));
    }
}
