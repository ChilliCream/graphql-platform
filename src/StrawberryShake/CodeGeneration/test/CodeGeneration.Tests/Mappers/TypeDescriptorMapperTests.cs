using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using RequestStrategyGen = StrawberryShake.Tools.Configuration.RequestStrategy;
using static StrawberryShake.CodeGeneration.Mappers.TestDataHelper;

namespace StrawberryShake.CodeGeneration.Mappers;

public class TypeDescriptorMapperTests
{
    [Fact]
    public async Task MapClientTypeDescriptors()
    {
        // arrange
        var clientModel = await CreateClientModelAsync(
            @"query GetHero {
                    hero(episode: NEW_HOPE) {
                        name
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
        TypeDescriptorMapper.Map(clientModel, context);

        // assert
        Assert.Collection(
            context.Types.OfType<ComplexTypeDescriptor>().OrderBy(t => t.Name),
            type =>
            {
                Assert.Equal("IGetHero_Hero", type.RuntimeType.Name);
                Assert.Equal("Foo.Bar", type.RuntimeType.NamespaceWithoutGlobal);
                Assert.True(type.IsEntity());

                Assert.Collection(
                    type.Properties,
                    property =>
                    {
                        Assert.Equal("Name", property.Name);
                        Assert.Equal("String", property.Type.Name);
                        Assert.False(property.Type.IsNullable());
                    });
            },
            type =>
            {
                Assert.Equal("GetHero_Hero_Droid", type.RuntimeType.Name);
                Assert.Equal("Foo.Bar", type.RuntimeType.NamespaceWithoutGlobal);

                Assert.Collection(
                    type.Properties,
                    property =>
                    {
                        Assert.Equal("Name", property.Name);
                        Assert.Equal("String", property.Type.Name);
                        Assert.False(property.Type.IsNullable());
                    });
            },
            type =>
            {
                Assert.Equal("IGetHero_Hero_Droid", type.RuntimeType.Name);
                Assert.Equal("Foo.Bar", type.RuntimeType.NamespaceWithoutGlobal);
                Assert.True(type.IsEntity());
            },
            type =>
            {
                Assert.Equal("GetHero_Hero_Human", type.RuntimeType.Name);
                Assert.Equal("Foo.Bar", type.RuntimeType.NamespaceWithoutGlobal);

                Assert.Collection(
                    type.Properties,
                    property =>
                    {
                        Assert.Equal("Name", property.Name);
                        Assert.Equal("String", property.Type.Name);
                        Assert.False(property.Type.IsNullable());
                    });
            },
            type =>
            {
                Assert.Equal("IGetHero_Hero_Human", type.RuntimeType.Name);
                Assert.Equal("Foo.Bar", type.RuntimeType.NamespaceWithoutGlobal);
                Assert.True(type.IsEntity());
            },
            type =>
            {
                Assert.Equal("GetHeroResult", type.RuntimeType.Name);
                Assert.Equal("Foo.Bar", type.RuntimeType.NamespaceWithoutGlobal);

                Assert.Collection(
                    type.Properties,
                    property =>
                    {
                        Assert.Equal("Hero", property.Name);
                        Assert.Equal("IGetHero_Hero",
                            Assert.IsType<InterfaceTypeDescriptor>(property.Type)
                                .RuntimeType.Name);
                        Assert.True(property.Type.IsNullable());
                    });
            },
            type =>
            {
                Assert.Equal("IGetHeroResult", type.RuntimeType.Name);
                Assert.Equal("Foo.Bar", type.RuntimeType.NamespaceWithoutGlobal);

                Assert.Collection(
                    type.Properties,
                    property =>
                    {
                        Assert.Equal("Hero", property.Name);
                        Assert.Equal("IGetHero_Hero",
                            Assert.IsType<InterfaceTypeDescriptor>(property.Type)
                                .RuntimeType.Name);
                        Assert.True(property.Type.IsNullable());
                    });
            });
    }
}
