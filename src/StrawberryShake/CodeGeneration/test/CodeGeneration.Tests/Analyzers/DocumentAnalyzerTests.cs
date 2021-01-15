using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using StrawberryShake.CodeGeneration.Analyzers.Models;
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
                    .TryAddTypeInterceptor(
                        new LeafTypeInterceptor(
                            new Dictionary<NameString, ScalarInfo>()))
                    .BuildSchemaAsync();

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
                type => Assert.Equal("String", type.Name));

            Assert.Collection(
                clientModel.Operations,
                op =>
                {
                    Assert.Equal("IGetHero", op.ResultType.Name);

                    Assert.Collection(
                        op.GetImplementations(op.ResultType),
                        model => Assert.Equal("GetHero", model.Name));
                });
        }
    }
}
