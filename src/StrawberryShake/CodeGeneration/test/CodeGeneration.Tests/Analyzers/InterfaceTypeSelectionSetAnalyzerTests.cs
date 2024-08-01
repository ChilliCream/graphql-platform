using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class InterfaceTypeSelectionSetAnalyzerTests
{
    [Fact]
    public async Task Interface_With_Default_Names_One_Models()
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
        var selectionSetVariants = context.CollectFields();
        var fieldSelection = selectionSetVariants.ReturnType.Fields.First();
        selectionSetVariants = context.CollectFields(fieldSelection);

        // act
        var analyzer = new InterfaceTypeSelectionSetAnalyzer();
        var result = analyzer.Analyze(context, fieldSelection, selectionSetVariants);

        // assert
        Assert.Equal("IGetHero_Hero", result.Name);

        Assert.Collection(
            context.GetImplementations(result).OrderBy(m => m.Name),
            model => Assert.Equal("IGetHero_Hero_Droid", model.Name),
            model => Assert.Equal("IGetHero_Hero_Human", model.Name));

        Assert.Collection(
            result.Fields,
            field => Assert.Equal("Name", field.Name));
    }

    [Fact]
    public async Task Interface_With_Default_Names_Two_Models()
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
                        ... on Droid {
                            primaryFunction
                        }
                    }
                }");

        var context = new DocumentAnalyzerContext(schema, document);
        var selectionSetVariants = context.CollectFields();
        var fieldSelection = selectionSetVariants.ReturnType.Fields.First();
        selectionSetVariants = context.CollectFields(fieldSelection);

        // act
        var analyzer = new InterfaceTypeSelectionSetAnalyzer();
        var result = analyzer.Analyze(context, fieldSelection, selectionSetVariants);

        // assert
        Assert.Equal("IGetHero_Hero", result.Name);

        Assert.Collection(
            context.GetImplementations(result),
            model => Assert.Equal("IGetHero_Hero_Human", model.Name),
            model => Assert.Equal("IGetHero_Hero_Droid", model.Name));

        Assert.Collection(
            result.Fields,
            field => Assert.Equal("Name", field.Name));
    }

    [Fact]
    public async Task Interface_With_Fragment_Definition_One_Model()
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
        var selectionSetVariants = context.CollectFields();
        var fieldSelection = selectionSetVariants.ReturnType.Fields.First();
        selectionSetVariants = context.CollectFields(fieldSelection);

        // act
        var analyzer = new InterfaceTypeSelectionSetAnalyzer();
        var result = analyzer.Analyze(context, fieldSelection, selectionSetVariants);

        // assert
        Assert.Equal("IGetHero_Hero", result.Name);

        Assert.Collection(
            context.GetImplementations(result).OrderBy(t => t.Name),
            model => Assert.Equal("IGetHero_Hero_Droid", model.Name),
            model => Assert.Equal("IGetHero_Hero_Human", model.Name));

        Assert.Empty(result.Fields);
    }

    [Fact]
    public async Task Interface_With_Fragment_Definition_Two_Models()
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
                    ... Human
                    ... Droid
                }

                fragment Human on Human {
                    homePlanet
                }

                fragment Droid on Droid {
                    primaryFunction
                }");

        var context = new DocumentAnalyzerContext(schema, document);
        var selectionSetVariants = context.CollectFields();
        var fieldSelection = selectionSetVariants.ReturnType.Fields.First();
        selectionSetVariants = context.CollectFields(fieldSelection);

        // act
        var analyzer = new InterfaceTypeSelectionSetAnalyzer();
        var result = analyzer.Analyze(context, fieldSelection, selectionSetVariants);

        // assert
        Assert.Equal("IGetHero_Hero", result.Name);

        Assert.Collection(
            context.GetImplementations(result),
            model => Assert.Equal("IGetHero_Hero_Human", model.Name),
            model => Assert.Equal("IGetHero_Hero_Droid", model.Name));

        Assert.Empty(result.Fields);
    }

    [Fact]
    public async Task Union_With_Fragment_Definition_Two_Models()
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
                    search(text: ""hello"") {
                        ... Hero
                        ... Starship
                    }
                }

                fragment Hero on Character {
                    name
                    ... Human
                    ... Droid
                }

                fragment Human on Human {
                    homePlanet
                }

                fragment Droid on Droid {
                    primaryFunction
                }

                fragment Starship on Starship {
                    length
                }");

        var context = new DocumentAnalyzerContext(schema, document);
        var selectionSetVariants = context.CollectFields();
        var fieldSelection = selectionSetVariants.ReturnType.Fields.First();
        selectionSetVariants = context.CollectFields(fieldSelection);

        // act
        var analyzer = new InterfaceTypeSelectionSetAnalyzer();
        var result = analyzer.Analyze(context, fieldSelection, selectionSetVariants);

        // assert
        Assert.Equal("IGetHero_Search", result.Name);

        Assert.Collection(
            context.GetImplementations(result),
            model => Assert.Equal("IGetHero_Search_Starship", model.Name),
            model => Assert.Equal("IGetHero_Search_Human", model.Name),
            model => Assert.Equal("IGetHero_Search_Droid", model.Name));

        Assert.Empty(result.Fields);
    }
}
