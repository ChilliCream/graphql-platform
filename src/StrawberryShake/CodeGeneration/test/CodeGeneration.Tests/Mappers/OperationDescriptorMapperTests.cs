using HotChocolate.Language;
using RequestStrategyGen = StrawberryShake.Tools.Configuration.RequestStrategy;
using static StrawberryShake.CodeGeneration.Mappers.TestDataHelper;

namespace StrawberryShake.CodeGeneration.Mappers;

public class OperationDescriptorMapperTests
{
    [Fact]
    public async Task MapOperationTypeDescriptors()
    {
        // arrange
        var clientModel = await CreateClientModelAsync(
            @"
                query GetHero {
                    hero(episode: NEW_HOPE) {
                        name
                        appearsIn
                    }
                }

                mutation CreateReview {
                    createReview(episode: NEW_HOPE, review: {stars: 5, commentary: ""splendid""}) {
                        stars
                        commentary
                    }
                }

                subscription OnReview {
                    onReview(episode: NEW_HOPE) {
                        stars
                        commentary
                    }
                }
            ");

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
        OperationDescriptorMapper.Map(clientModel, context);

        // assert
        Assert.Collection(
            context.Operations.OrderBy(t => t.Name),
            operation =>
            {
                Assert.Equal("CreateReview", operation.Name);
            },
            operation =>
            {
                Assert.Equal("GetHero", operation.Name);
            },
            operation =>
            {
                Assert.Equal("OnReview", operation.Name);
            });
    }
}
