using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using RequestStrategyGen = StrawberryShake.Tools.Configuration.RequestStrategy;
using static StrawberryShake.CodeGeneration.Mappers.TestDataHelper;

namespace StrawberryShake.CodeGeneration.Mappers;

public class EnumDescriptorMapperTests
{
    [Fact]
    public async Task MapEnumTypeDescriptors()
    {
        // arrange
        var clientModel = await CreateClientModelAsync(
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
            RequestStrategyGen.Default,
            new[]
            {
                TransportProfile.Default,
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
