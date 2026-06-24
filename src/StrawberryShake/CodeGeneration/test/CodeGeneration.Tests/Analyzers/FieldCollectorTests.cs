using HotChocolate.Language;
using HotChocolate.Types;
using Path = HotChocolate.Path;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class FieldCollectorTests
{
    [Fact]
    public async Task Collect_First_Level_No_Fragments()
    {
        // arrange
        var schema = await TestSchemaHelper.CreateStarWarsSchemaAsync();

        var document =
            Utf8GraphQLParser.Parse(@"
                {
                    hero(episode: NEW_HOPE) {
                        name
                        ... on Droid {
                            primaryFunction
                        }
                    }
                }");

        var operation = document
            .Definitions
            .OfType<OperationDefinitionNode>()
            .First();
        var queryType = schema.QueryType
            ?? throw new InvalidOperationException("The Star Wars schema must define a query type.");

        // act
        var selectionSetVariants =
            new FieldCollector(schema, document)
                .CollectFields(operation.SelectionSet, queryType, Path.Root);

        // assert
        Assert.Collection(
            selectionSetVariants.ReturnType.Fields,
            field => Assert.Equal("hero", field.ResponseName));
    }

    [Fact]
    public async Task Collect_Second_Level_Fragments()
    {
        // arrange
        var schema = await TestSchemaHelper.CreateStarWarsSchemaAsync();

        var character = schema.Types.GetType<IInterfaceTypeDefinition>("Character")
            ?? throw new InvalidOperationException("The Star Wars schema must define Character.");

        var document =
            Utf8GraphQLParser.Parse(@"
                {
                    hero(episode: NEW_HOPE) {
                        name
                        ... on Droid {
                            primaryFunction
                        }
                    }
                }");

        var operation = document
            .Definitions
            .OfType<OperationDefinitionNode>()
            .First();

        var secondLevel = operation
            .SelectionSet
            .Selections
            .OfType<FieldNode>()
            .First();

        // act
        var selectionSetVariants =
            new FieldCollector(schema, document)
                .CollectFields(
                    secondLevel.SelectionSet!,
                    character,
                    Path.Root.Append("hero"));

        // assert
        Assert.Collection(
            selectionSetVariants.ReturnType.Fields,
            field => Assert.Equal("name", field.ResponseName));
        Assert.Equal("Character", selectionSetVariants.ReturnType.Type.Name);
        Assert.Equal("Droid", selectionSetVariants.Variants[0].Type.Name);
        Assert.Equal("Human", selectionSetVariants.Variants[1].Type.Name);

        Assert.Collection(
            selectionSetVariants.Variants[0].FragmentNodes,
            fragmentNode => Assert.Equal(FragmentKind.Inline, fragmentNode.Fragment.Kind));
    }
}
