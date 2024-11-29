using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class FragmentHelperTests
{
    [Fact]
    public async Task GetReturnTypeName_Found()
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

        var context = new DocumentAnalyzerContext(schema, document);
        var selectionSetVariants = context.CollectFields();
        var fieldSelection = selectionSetVariants.ReturnType.Fields.First();

        // act
        var returnTypeFragmentName = FragmentHelper.GetReturnTypeName(fieldSelection);

        // assert
        Assert.Equal("Hero", returnTypeFragmentName);
    }

    [Fact]
    public async Task GetReturnTypeName_Not_Found()
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

        var context = new DocumentAnalyzerContext(schema, document);
        var selectionSetVariants = context.CollectFields();
        var fieldSelection = selectionSetVariants.ReturnType.Fields.First();

        // act
        var returnTypeFragmentName = FragmentHelper.GetReturnTypeName(fieldSelection);

        // assert
        Assert.Null(returnTypeFragmentName);
    }

    [Fact]
    public async Task GetFragment_From_FragmentTree_Found()
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
                    hero(episode: NEW_HOPE) @returns(fragment: ""Hero"") {
                        ... Characters
                    }
                }

                fragment Characters on Character @remove {
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

        var context = new DocumentAnalyzerContext(schema, document);
        var selectionSetVariants = context.CollectFields();
        var fieldSelection = selectionSetVariants.ReturnType.Fields.First();
        selectionSetVariants = context.CollectFields(fieldSelection);

        // act
        var returnTypeFragmentName = FragmentHelper.GetReturnTypeName(fieldSelection);
        var returnTypeFragment = FragmentHelper.CreateFragmentNode(
            selectionSetVariants.Variants[0],
            fieldSelection.Path,
            appendTypeName: true);
        returnTypeFragment = FragmentHelper.GetFragment(
            returnTypeFragment,
            returnTypeFragmentName!);

        // assert
        Assert.NotNull(returnTypeFragment);
        Assert.Equal("Hero", returnTypeFragment?.Fragment.Name);
    }

    [Fact]
    public async Task Create_TypeModels_Infer_TypeStructure()
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
        var list = new List<OutputTypeModel>();
        var returnTypeFragment = FragmentHelper.CreateFragmentNode(
            selectionSetVariants.ReturnType,
            fieldSelection.Path);
        list.Add(FragmentHelper.CreateInterface(
            context,
            returnTypeFragment,
            fieldSelection.Path));

        foreach (var selectionSet in
                 selectionSetVariants.Variants.OrderBy(t => t.Type.Name))
        {
            returnTypeFragment = FragmentHelper.CreateFragmentNode(
                selectionSet,
                fieldSelection.Path,
                appendTypeName: true);

            var @interface = FragmentHelper.CreateInterface(
                context,
                returnTypeFragment,
                fieldSelection.Path,
                new []{ list[0], });

            var @class = FragmentHelper.CreateClass(
                context,
                returnTypeFragment,
                selectionSet,
                @interface);

            list.Add(@interface);
            list.Add(@class);
        }

        // assert
        Assert.Collection(
            list,
            type =>
            {
                Assert.Equal("IGetHero_Hero", type.Name);

                Assert.Empty(type.Implements);

                Assert.Collection(
                    type.Fields,
                    field => Assert.Equal("Name", field.Name));
            },
            type =>
            {
                Assert.Equal("IGetHero_Hero_Droid", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("IGetHero_Hero", impl.Name));

                Assert.Empty(type.Fields);
            },
            type =>
            {
                Assert.Equal("GetHero_Hero_Droid", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("IGetHero_Hero_Droid", impl.Name));

                Assert.Collection(
                    type.Fields,
                    field => Assert.Equal("Name", field.Name));
            },
            type =>
            {
                Assert.Equal("IGetHero_Hero_Human", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("IGetHero_Hero", impl.Name));

                Assert.Empty(type.Fields);
            },
            type =>
            {
                Assert.Equal("GetHero_Hero_Human", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("IGetHero_Hero_Human", impl.Name));

                Assert.Collection(
                    type.Fields,
                    field => Assert.Equal("Name", field.Name));
            });
    }

    [Fact]
    public async Task Create_TypeModels_Infer_From_Fragments()
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
        var list = new List<OutputTypeModel>();
        var returnTypeFragment = FragmentHelper.CreateFragmentNode(
            selectionSetVariants.ReturnType,
            fieldSelection.Path);
        list.Add(FragmentHelper.CreateInterface(
            context,
            returnTypeFragment,
            fieldSelection.Path));

        foreach (var selectionSet in
                 selectionSetVariants.Variants.OrderBy(t => t.Type.Name))
        {
            returnTypeFragment = FragmentHelper.CreateFragmentNode(
                selectionSet,
                fieldSelection.Path,
                appendTypeName: true);

            var @interface = FragmentHelper.CreateInterface(
                context,
                returnTypeFragment,
                fieldSelection.Path,
                new []{ list[0], });

            var @class = FragmentHelper.CreateClass(
                context,
                returnTypeFragment,
                selectionSet,
                @interface);

            list.Add(@interface);
            list.Add(@class);
        }

        // assert
        Assert.Collection(
            list,
            type =>
            {
                Assert.Equal("IGetHero_Hero", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("IHero", impl.Name));

                Assert.Empty(type.Fields);
            },
            type =>
            {
                Assert.Equal("IGetHero_Hero_Droid", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("IGetHero_Hero", impl.Name),
                    impl => Assert.Equal("IDroid", impl.Name));

                Assert.Empty(type.Fields);
            },
            type =>
            {
                Assert.Equal("GetHero_Hero_Droid", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("IGetHero_Hero_Droid", impl.Name));

                Assert.Collection(
                    type.Fields,
                    field => Assert.Equal("Name", field.Name),
                    field => Assert.Equal("PrimaryFunction", field.Name));
            },
            type =>
            {
                Assert.Equal("IGetHero_Hero_Human", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("IGetHero_Hero", impl.Name),
                    impl => Assert.Equal("IHuman", impl.Name));

                Assert.Empty(type.Fields);
            },
            type =>
            {
                Assert.Equal("GetHero_Hero_Human", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("IGetHero_Hero_Human", impl.Name));

                Assert.Collection(
                    type.Fields,
                    field => Assert.Equal("Name", field.Name),
                    field => Assert.Equal("HomePlanet", field.Name));
            });
    }

    [Fact]
    public async Task Create_TypeModels_Infer_From_Fragments_With_HoistedFragment()
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

        var context = new DocumentAnalyzerContext(schema, document);
        var selectionSetVariants = context.CollectFields();
        var fieldSelection = selectionSetVariants.ReturnType.Fields.First();
        selectionSetVariants = context.CollectFields(fieldSelection);

        // act
        var list = new List<OutputTypeModel>();
        var returnTypeFragmentName = FragmentHelper.GetReturnTypeName(fieldSelection);
        var returnTypeFragment = FragmentHelper.CreateFragmentNode(
            selectionSetVariants.Variants[0],
            fieldSelection.Path,
            appendTypeName: true);
        returnTypeFragment = FragmentHelper.GetFragment(
            returnTypeFragment,
            returnTypeFragmentName!);
        list.Add(FragmentHelper.CreateInterface(
            context,
            returnTypeFragment!,
            fieldSelection.Path));

        foreach (var selectionSet in
                 selectionSetVariants.Variants.OrderBy(t => t.Type.Name))
        {
            returnTypeFragment = FragmentHelper.CreateFragmentNode(
                selectionSet,
                fieldSelection.Path,
                appendTypeName: true);

            returnTypeFragment = FragmentHelper.RewriteForConcreteType(returnTypeFragment);

            var @interface = FragmentHelper.CreateInterface(
                context,
                returnTypeFragment,
                fieldSelection.Path,
                new []{ list[0], });

            var @class = FragmentHelper.CreateClass(
                context,
                returnTypeFragment,
                selectionSet,
                @interface);

            list.Add(@interface);
            list.Add(@class);
        }

        // assert
        Assert.Collection(
            list,
            type =>
            {
                Assert.Equal("IHero", type.Name);

                Assert.Empty(type.Implements);

                Assert.Collection(
                    type.Fields,
                    field => Assert.Equal("Name", field.Name));
            },
            type =>
            {
                Assert.Equal("IGetHero_Hero_Droid", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("ICharacters_Droid", impl.Name));

                Assert.Empty(type.Fields);
            },
            type =>
            {
                Assert.Equal("GetHero_Hero_Droid", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("IGetHero_Hero_Droid", impl.Name));

                Assert.Collection(
                    type.Fields,
                    field => Assert.Equal("Name", field.Name),
                    field => Assert.Equal("PrimaryFunction", field.Name));
            },
            type =>
            {
                Assert.Equal("IGetHero_Hero_Human", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("ICharacters_Human", impl.Name));

                Assert.Empty(type.Fields);
            },
            type =>
            {
                Assert.Equal("GetHero_Hero_Human", type.Name);

                Assert.Collection(
                    type.Implements,
                    impl => Assert.Equal("IGetHero_Hero_Human", impl.Name));

                Assert.Collection(
                    type.Fields,
                    field => Assert.Equal("Name", field.Name),
                    field => Assert.Equal("HomePlanet", field.Name));
            });
    }
}
