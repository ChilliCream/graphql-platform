using System.Threading.Tasks;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using Xunit;
using static StrawberryShake.CodeGeneration.Mappers.TestDataHelper;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public class ClientDescriptorMapperTests
    {
        [Fact]
        public async Task MapClientDescriptor()
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
            var clientName = "FooClient";
            var context = new MapperContext(
                "Foo.Bar",
                clientName,
                new Sha1DocumentHashProvider(),
                Descriptors.Operations.RequestStrategy.Default);
            TypeDescriptorMapper.Map(clientModel, context);
            OperationDescriptorMapper.Map(clientModel, context);
            ClientDescriptorMapper.Map(clientModel, context);

            // assert
            Assert.Equal(clientName, context.Client.Name);
            Assert.Equal(3, context.Client.Operations.Count);
        }
    }
}
