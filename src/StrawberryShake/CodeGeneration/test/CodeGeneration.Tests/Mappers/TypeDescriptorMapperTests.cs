using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Extensions;
using StrawberryShake.CodeGeneration.Utilities;
using Xunit;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public class TypeDescriptorMapperTests
    {
        [Fact]
        public async Task MapClientTypeDescriptors()
        {
            // arrange
            ClientModel clientModel = await CreateClientModelAsync(
                @"query GetHero {
                    hero(episode: NEW_HOPE) {
                        name
                    }
                }");

            // act
            var context = new MapperContext("Foo.Bar");
            TypeDescriptorMapper.Map(clientModel, context);

            // assert
            Assert.Collection(
                context.Types.OrderBy(t => t.Name),
                type =>
                {
                    Assert.Equal("GetHero", type.Name);
                    Assert.Equal("Foo.Bar", type.Namespace);

                    Assert.Collection(
                        type.Properties,
                        property => 
                        {
                            Assert.Equal("Hero", property.Name);
                            Assert.Equal("IGetHero_Hero", property.Type.Name);
                            Assert.True(property.Type.IsNullableType());
                        });
                },
                type => 
                { 
                    Assert.Equal("GetHero_Hero", type.Name);
                    Assert.Equal("Foo.Bar", type.Namespace);

                    Assert.Collection(
                        type.Properties,
                        property => 
                        {
                            Assert.Equal("Name", property.Name);
                            Assert.Equal("String", property.Type.Name);
                            Assert.False(property.Type.IsNullableType());
                        });
                },
                type =>
                {
                    Assert.Equal("IGetHero", type.Name);
                    Assert.Equal("Foo.Bar", type.Namespace);

                    Assert.Collection(
                        type.Properties,
                        property => 
                        {
                            Assert.Equal("Hero", property.Name);
                            Assert.Equal("IGetHero_Hero", property.Type.Name);
                            Assert.True(property.Type.IsNullableType());
                        });
                },
                type => 
                { 
                    Assert.Equal("IGetHero_Hero", type.Name);
                    Assert.Equal("Foo.Bar", type.Namespace);

                    Assert.Collection(
                        type.Properties,
                        property => 
                        {
                            Assert.Equal("Name", property.Name);
                            Assert.Equal("String", property.Type.Name);
                            Assert.False(property.Type.IsNullableType());
                        });
                },
                type => 
                { 
                    Assert.Equal("String", type.Name);
                    Assert.Equal("System", type.Namespace);
                    Assert.True(type.IsLeafType());
                });
        }

        private async Task<ClientModel> CreateClientModelAsync(string query)
        {
            ISchema schema =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWars()
                    .BuildSchemaAsync();

            schema = SchemaHelper.Load(schema.ToDocument());

            DocumentNode document = Utf8GraphQLParser.Parse(query);

            return DocumentAnalyzer
                .New()
                .SetSchema(schema)
                .AddDocument(document)
                .Analyze();
        }
    }
}
