using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using Xunit;
using static StrawberryShake.CodeGeneration.Mappers.TestDataHelper;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public class EnumDescriptorMapperTests
    {
        [Fact]
        public async Task MapEnumTypeDescriptors()
        {
            // arrange
            ClientModel clientModel = await CreateClientModelAsync(
                @"query GetHero {
                    hero(episode: NEW_HOPE) {
                        name
                        appearsIn
                    }
                }");

            // act
            var context = new MapperContext("Foo.Bar", "FooClient");
            EnumDescriptorMapper.Map(clientModel, context);

            // assert
            Assert.Collection(
                context.EnumTypes.OrderBy(t => t.Name),
                enumType =>
                {
                    Assert.Equal("Episode", enumType.Name);

                    Assert.Collection(
                        enumType.Values.OrderBy(t => t.Name),
                        value =>
                        {
                            Assert.Equal("Empire", value.Name);
                            Assert.Null(value.Value);
                        },
                        value =>
                        {
                            Assert.Equal("Jedi", value.Name);
                            Assert.Null(value.Value);
                        },
                        value =>
                        {
                            Assert.Equal("NewHope", value.Name);
                            Assert.Null(value.Value);
                        });
                });
        }
    }
}
