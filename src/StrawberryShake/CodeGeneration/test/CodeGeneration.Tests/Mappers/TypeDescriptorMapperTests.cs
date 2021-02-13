using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Extensions;
using Xunit;
using static StrawberryShake.CodeGeneration.Mappers.TestDataHelper;

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
            var context = new MapperContext("Foo.Bar", "FooClient");
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
                    Assert.Equal("GetHero_Hero_Droid", type.Name);
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
                    Assert.Equal("GetHero_Hero_Human", type.Name);
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
                    Assert.True(type.IsEntityType());

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
                    Assert.Equal("IGetHero_Hero_Droid", type.Name);
                    Assert.Equal("Foo.Bar", type.Namespace);
                    Assert.True(type.IsEntityType());
                },
                type =>
                {
                    Assert.Equal("IGetHero_Hero_Human", type.Name);
                    Assert.Equal("Foo.Bar", type.Namespace);
                    Assert.True(type.IsEntityType());
                },
                type =>
                {
                    Assert.Equal("String", type.Name);
                    Assert.Equal("global::System", type.Namespace);
                    Assert.True(type.IsLeafType());
                });
        }
    }
}
