using HotChocolate.Language;
using RequestStrategyGen = StrawberryShake.Tools.Configuration.RequestStrategy;
using static StrawberryShake.CodeGeneration.Mappers.TestDataHelper;

namespace StrawberryShake.CodeGeneration.Mappers;

public class ResultBuilderDescriptorMapperTests
{
    [Fact]
    public async Task MapResultBuilderDescriptors()
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
        ResultBuilderDescriptorMapper.Map(clientModel, context);

        // assert
        Assert.Collection(
            context.ResultBuilders.OrderBy(t => t.RuntimeType.ToString()),
            resultBuilder =>
            {
                Assert.Equal("CreateReviewBuilder", resultBuilder.RuntimeType.Name);
            },
            resultBuilder =>
            {
                Assert.Equal("GetHeroBuilder", resultBuilder.RuntimeType.Name);
            },
            resultBuilder =>
            {
                Assert.Equal("OnReviewBuilder", resultBuilder.RuntimeType.Name);
            });
    }
}
