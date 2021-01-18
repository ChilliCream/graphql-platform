using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using Xunit;

namespace StrawberryShake.CodeGeneration.Analyzers
{
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
                    new[]
                    {
                        schema.ToDocument(),
                        Utf8GraphQLParser.Parse(
                            @"extend scalar String @runtimeType(name: ""Abc"")")
                    });

            var document =
                Utf8GraphQLParser.Parse(@"
                    query GetHero {
                        hero(episode: NEW_HOPE) {
                            name
                        }
                    }");

            // act
            ClientModel clientModel =
                DocumentAnalyzer
                    .New()
                    .SetSchema(schema)
                    .AddDocument(document)
                    .Analyze();

            // assert
            Assert.Empty(
                clientModel.InputObjectTypes);

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

                    OutputTypeModel fieldResultType =
                        op.GetFieldResultType(op.ResultType.Fields.Single().SyntaxNode);
                    Assert.Equal("IGetHero_Hero", fieldResultType.Name);
                });
        }
    }
}
