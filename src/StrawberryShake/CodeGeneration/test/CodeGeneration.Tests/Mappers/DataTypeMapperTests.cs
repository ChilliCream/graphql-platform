using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using Xunit;
using static StrawberryShake.CodeGeneration.Mappers.TestDataHelper;
using static ChilliCream.Testing.FileResource;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public class DataTypeMapperTests
    {
        [Fact]
        public async Task MapDataTypeDescriptors_SimpleCase()
        {
            // arrange
            ClientModel clientModel = await CreateClientModelAsync(
                @"
                    query GetHeroNodes {
                      hero(episode: NEW_HOPE) {
                        friends {
                          nodes {
                            name
                          }
                        }
                      }
                    }

                    query GetHeroEdges {
                      hero(episode: NEW_HOPE) {
                        friends {
                          edges {
                            cursor
                          }
                        }
                      }
                    }");

            // act
            var context = new MapperContext(
                "Foo.Bar",
                "FooClient");
            TypeDescriptorMapper.Map(
                clientModel,
                context);
            DataTypeDescriptorMapper.Map(
                clientModel,
                context);

            // assert
            Assert.Collection(
                context.DataTypes.OrderBy(t => t.Name),
                type =>
                {
                    Assert.Equal(
                        "CharacterConnectionData",
                        type.Name);
                    Assert.Equal(
                        "Foo.Bar.State",
                        type.Namespace);

                    Assert.Collection(
                        type.Properties,
                        property =>
                        {
                            Assert.Equal(
                                "Nodes",
                                property.Name);
                            Assert.Equal(
                                "IGetHeroNodes_Hero_Friends_Nodes",
                                property.Type.Name);
                        },
                        property =>
                        {
                            Assert.Equal(
                                "Edges",
                                property.Name);
                            Assert.Equal(
                                "IGetHeroEdges_Hero_Friends_Edges",
                                property.Type.Name);
                        });
                },
                type =>
                {
                    Assert.Equal(
                        "CharacterEdgeData",
                        type.Name);
                    Assert.Equal(
                        "Foo.Bar.State",
                        type.Namespace);

                    Assert.Collection(
                        type.Properties,
                        property =>
                        {
                            Assert.Equal(
                                "Cursor",
                                property.Name);
                            Assert.Equal(
                                "String",
                                property.Type.Name);
                        });
                });
        }

        [Fact]
        public async Task MapDataTypeDescriptors_DataUnionType()
        {
            // arrange
            var clientModel = CreateClientModelAsync("union.query3.graphql", "union.schema.graphql");

            // act
            var context = new MapperContext(
                "Foo.Bar",
                "FooClient");
            TypeDescriptorMapper.Map(
                clientModel,
                context);
            EntityTypeDescriptorMapper.Map(
                clientModel,
                context);
            DataTypeDescriptorMapper.Map(
                clientModel,
                context);

            // assert

            Assert.Collection(
                context.DataTypes.OrderBy(t => t.Name),
                type =>
                {
                    Assert.Equal(
                        "AuthorData",
                        type.Name);
                    Assert.Equal(
                        "Foo.Bar.State",
                        type.Namespace);

                    Assert.Collection(
                        type.Properties.OrderBy(p => p.Name),
                        property =>
                        {
                            Assert.Equal(
                                "Genres",
                                property.Name);
                        },
                        property =>
                        {
                            Assert.Equal(
                                "Name",
                                property.Name);
                        });
                },
                type =>
                {
                    Assert.Equal(
                        "BookData",
                        type.Name);
                    Assert.Equal(
                        "Foo.Bar.State",
                        type.Namespace);

                    Assert.Collection(
                        type.Properties.OrderBy(p => p.Name),
                        property =>
                        {
                            Assert.Equal(
                                "Isbn",
                                property.Name);
                        },
                        property =>
                        {
                            Assert.Equal(
                                "Title",
                                property.Name);
                        });
                },
                type =>
                {
                    Assert.Equal(
                        "ISearchResultData",
                        type.Name);
                    Assert.Equal(
                        "Foo.Bar.State",
                        type.Namespace);

                    Assert.Empty(type.Properties);
                });
        }


        [Fact]
        public async Task MapDataTypeDescriptors_DataInterfaceType()
        {
            // arrange
            var clientModel = CreateClientModelAsync("interface.query.graphql", "interface.schema.graphql");

            // act
            var context = new MapperContext(
                "Foo.Bar",
                "FooClient");
            TypeDescriptorMapper.Map(
                clientModel,
                context);
            EntityTypeDescriptorMapper.Map(
                clientModel,
                context);
            DataTypeDescriptorMapper.Map(
                clientModel,
                context);

            // assert
            Assert.Collection(
                context.DataTypes.OrderBy(t => t.Name),
                type =>
                {
                    Assert.Equal(
                        "BookData",
                        type.Name);
                    Assert.Equal(
                        "Foo.Bar.State",
                        type.Namespace);

                    Assert.Collection(
                        type.Properties.OrderBy(p => p.Name),
                        property =>
                        {
                            Assert.Equal(
                                "Isbn",
                                property.Name);
                        },
                        property =>
                        {
                            Assert.Equal(
                                "Title",
                                property.Name);
                        });
                },
                type =>
                {
                    Assert.Equal(
                        "IPrintData",
                        type.Name);
                    Assert.Equal(
                        "Foo.Bar.State",
                        type.Namespace);

                    Assert.Empty(type.Properties);
                },
                type =>
                {
                    Assert.Equal(
                        "ISearchResultData",
                        type.Name);
                    Assert.Equal(
                        "Foo.Bar.State",
                        type.Namespace);

                    Assert.Empty(type.Properties);
                },
                type =>
                {
                    Assert.Equal(
                        "MagazineData",
                        type.Name);
                    Assert.Equal(
                        "Foo.Bar.State",
                        type.Namespace);

                    Assert.Collection(
                        type.Properties.OrderBy(p => p.Name),
                        property =>
                        {
                            Assert.Equal(
                                "CoverImageUrl",
                                property.Name);
                        },
                        property =>
                        {
                            Assert.Equal(
                                "Isbn",
                                property.Name);
                        });
                });
        }
    }
}
