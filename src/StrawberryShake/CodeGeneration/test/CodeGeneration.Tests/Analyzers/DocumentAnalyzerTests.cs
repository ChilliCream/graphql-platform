using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class DocumentAnalyzerTests
{
    [Fact]
    public async Task One_Document_One_Op_One_Field_No_Fragments()
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
                new GraphQLFile[]
                {
                    new(schema.ToDocument()),
                    new(Utf8GraphQLParser.Parse(
                        @"extend scalar String @runtimeType(name: ""Abc"")")),
                });

        var document =
            Utf8GraphQLParser.Parse(@"
                query GetHero {
                    hero(episode: NEW_HOPE) {
                        name
                    }
                }");

        // act
        var clientModel =
            await DocumentAnalyzer
                .New()
                .SetSchema(schema)
                .AddDocument(document)
                .AnalyzeAsync();

        // assert
        Assert.Empty(clientModel.InputObjectTypes);

        Assert.Collection(
            clientModel.LeafTypes,
            type =>
            {
                Assert.Equal("String", type.Name);
                Assert.Equal("Abc", type.RuntimeType);
            });

        Assert.Collection(
            clientModel.Operations,
            op =>
            {
                Assert.Equal("IGetHero", op.ResultType.Name);

                Assert.Collection(
                    op.GetImplementations(op.ResultType),
                    model => Assert.Equal("GetHero", model.Name));

                var fieldResultType = op.GetFieldResultType(
                    op.ResultType.Fields.Single().SyntaxNode);
                Assert.Equal("IGetHero_Hero", fieldResultType.Name);
            });
    }

    [Fact]
    public async Task One_Fragment_One_Deferred_Fragment()
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
                new GraphQLFile[]
                {
                    new(schema.ToDocument()),
                    new(Utf8GraphQLParser.Parse(
                        @"extend scalar String @runtimeType(name: ""Abc"")")),
                    new(Utf8GraphQLParser.Parse(
                        "extend schema @key(fields: \"id\")")),
                });

        var document =
            Utf8GraphQLParser.Parse(@"
                query GetHero {
                    hero(episode: NEW_HOPE) {
                        ... HeroName
                        ... HeroAppearsIn @defer(label: ""HeroAppearsIn"")
                    }
                }

                fragment HeroName on Character {
                    name
                }

                fragment HeroAppearsIn on Character {
                    appearsIn
                }");

        // act
        var clientModel =
            await DocumentAnalyzer
                .New()
                .SetSchema(schema)
                .AddDocument(document)
                .AnalyzeAsync();

        // assert
        var human = clientModel.OutputTypes.First(t => t.Name.EqualsOrdinal("GetHero_Hero_Human"));
        Assert.Single(human.Fields);

        Assert.True(
            human.Deferred.ContainsKey("HeroAppearsIn"),
            "Human does not contain deferred model `HeroAppearsIn`.");

        Assert.Collection(
            human.Deferred["HeroAppearsIn"].Class.Fields,
            field => Assert.Equal("AppearsIn", field.Name));
    }
}
