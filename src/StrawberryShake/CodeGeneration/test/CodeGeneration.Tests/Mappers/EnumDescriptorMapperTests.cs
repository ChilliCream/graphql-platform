using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
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
            var context = new MapperContext(
                "Foo.Bar",
                "FooClient",
                new Sha1DocumentHashProvider(),
                Descriptors.Operations.RequestStrategy.Default,
                new[]
                {
                    TransportProfile.Default
                });
            TypeDescriptorMapper.Map(clientModel, context);

            // assert
            Assert.Collection(
                context.Types.OfType<EnumTypeDescriptor>().OrderBy(t => t.Name),
                enumType =>
                {
                    Assert.Equal("Episode", enumType.Name);

                    Assert.Collection(
                        enumType.Values.OrderBy(t => t.RuntimeValue),
                        value =>
                        {
                            Assert.Equal("Empire", value.RuntimeValue);
                            Assert.Null(value.Value);
                        },
                        value =>
                        {
                            Assert.Equal("Jedi", value.RuntimeValue);
                            Assert.Null(value.Value);
                        },
                        value =>
                        {
                            Assert.Equal("NewHope", value.RuntimeValue);
                            Assert.Null(value.Value);
                        });
                });
        }
    }
}
