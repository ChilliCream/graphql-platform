using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Extensions;
using RequestStrategyGen = StrawberryShake.Tools.Configuration.RequestStrategy;
using static StrawberryShake.CodeGeneration.Mappers.TestDataHelper;

namespace StrawberryShake.CodeGeneration.Mappers;

public class DataTypeMapperTests
{
    [Fact]
    public async Task MapDataTypeDescriptors_SimpleCase()
    {
        // arrange
        var clientModel = await CreateClientModelAsync(
            @"query GetHeroNodes {
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
            "FooClient",
            new Sha1DocumentHashProvider(),
            RequestStrategyGen.Default,
            new[]
            {
                TransportProfile.Default,
            });

        TypeDescriptorMapper.Map(
            clientModel,
            context);
        DataTypeDescriptorMapper.Map(
            clientModel,
            context);

        // assert
        Assert.Collection(
            context.DataTypes.OrderBy(t => t.RuntimeType.ToString()),
            type =>
            {
                Assert.Equal(
                    "FriendsConnectionData",
                    type.RuntimeType.Name);
                Assert.Equal(
                    "Foo.Bar.State",
                    type.RuntimeType.NamespaceWithoutGlobal);

                Assert.Collection(
                    type.Properties,
                    property =>
                    {
                        Assert.Equal(
                            "Nodes",
                            property.Name);
                        Assert.Equal(
                            "IGetHeroNodes_Hero_Friends_Nodes",
                            property.Type.GetRuntimeType().Name);
                    },
                    property =>
                    {
                        Assert.Equal(
                            "Edges",
                            property.Name);
                        Assert.Equal(
                            "IGetHeroEdges_Hero_Friends_Edges",
                            property.Type.GetRuntimeType().Name);
                    });
            },
            type =>
            {
                Assert.Equal(
                    "FriendsEdgeData",
                    type.RuntimeType.Name);
                Assert.Equal(
                    "Foo.Bar.State",
                    type.RuntimeType.NamespaceWithoutGlobal);

                Assert.Collection(
                    type.Properties,
                    property =>
                    {
                        Assert.Equal(
                            "Cursor",
                            property.Name);
                        Assert.Equal(
                            "String",
                            property.Type.GetRuntimeType().Name);
                    });
            });
    }

    [Fact]
    public void MapDataTypeDescriptors_DataUnionType()
    {
        // arrange
        var clientModel =
            CreateClientModelAsync("union.query3.graphql", "union.schema.graphql");

        // act
        var context = new MapperContext(
            "Foo.Bar",
            "FooClient",
            new Sha1DocumentHashProvider(),
            RequestStrategyGen.Default,
            new[]
            {
                TransportProfile.Default,
            });
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
            context.DataTypes.OrderBy(t => t.RuntimeType.ToString()),
            type =>
            {
                Assert.Equal(
                    "AuthorData",
                    type.RuntimeType.Name);
                Assert.Equal(
                    "Foo.Bar.State",
                    type.RuntimeType.NamespaceWithoutGlobal);

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
                    type.RuntimeType.Name);
                Assert.Equal(
                    "Foo.Bar.State",
                    type.RuntimeType.NamespaceWithoutGlobal);

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
                    type.RuntimeType.Name);
                Assert.Equal(
                    "Foo.Bar.State",
                    type.RuntimeType.NamespaceWithoutGlobal);

                Assert.Empty(type.Properties);
            });
    }

    [Fact]
    public void MapDataTypeDescriptors_DataInterfaceType()
    {
        // arrange
        var clientModel = CreateClientModelAsync(
            "interface.query.graphql",
            "interface.schema.graphql");

        // act
        var context = new MapperContext(
            "Foo.Bar",
            "FooClient",
            new Sha1DocumentHashProvider(),
            RequestStrategyGen.Default,
            new[]
            {
                TransportProfile.Default,
            });
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
            context.DataTypes.OrderBy(t => t.RuntimeType.ToString()),
            type =>
            {
                Assert.Equal(
                    "BookData",
                    type.RuntimeType.Name);
                Assert.Equal(
                    "Foo.Bar.State",
                    type.RuntimeType.NamespaceWithoutGlobal);

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
                    type.RuntimeType.Name);
                Assert.Equal(
                    "Foo.Bar.State",
                    type.RuntimeType.NamespaceWithoutGlobal);

                Assert.Empty(type.Properties);
            },
            type =>
            {
                Assert.Equal(
                    "ISearchResultData",
                    type.RuntimeType.Name);
                Assert.Equal(
                    "Foo.Bar.State",
                    type.RuntimeType.NamespaceWithoutGlobal);

                Assert.Empty(type.Properties);
            },
            type =>
            {
                Assert.Equal(
                    "MagazineData",
                    type.RuntimeType.Name);
                Assert.Equal(
                    "Foo.Bar.State",
                    type.RuntimeType.NamespaceWithoutGlobal);

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
