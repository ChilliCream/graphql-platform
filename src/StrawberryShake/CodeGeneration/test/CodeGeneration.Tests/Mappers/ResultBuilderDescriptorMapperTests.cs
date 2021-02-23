using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using Xunit;
using static StrawberryShake.CodeGeneration.Mappers.TestDataHelper;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public class ResultBuilderDescriptorMapperTests
    {
        [Fact]
        public async Task MapResultBuilderDescriptors()
        {
            // arrange
            ClientModel clientModel = await CreateClientModelAsync(
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
            var context = new MapperContext("Foo.Bar", "FooClient");
            TypeDescriptorMapper.Map(clientModel, context);
            ResultBuilderDescriptorMapper.Map(clientModel, context);

            // assert
            Assert.Collection(
                context.ResultBuilders.OrderBy(t => t.RuntimeType),
                resultBuilder =>
                {
                    Assert.Equal("CreateReviewBuilder", resultBuilder.RuntimeType);
                },
                resultBuilder =>
                {
                    Assert.Equal("GetHeroBuilder", resultBuilder.RuntimeType);
                },
                resultBuilder =>
                {
                    Assert.Equal("OnReviewBuilder", resultBuilder.RuntimeType);
                });
        }
    }
}
