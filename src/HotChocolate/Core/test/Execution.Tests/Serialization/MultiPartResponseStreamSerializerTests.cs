using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Serialization
{
    public class MultiPartResponseStreamSerializerTests
    {
        [Fact]
        public async Task Serialize_Response_Stream()
        {
            // arrange
            IExecutionResult result =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWarsTypes()
                    .ExecuteRequestAsync(
                        @"{
                            hero(episode: NEW_HOPE) {
                                id
                                ... @defer(label: ""friends"") {
                                    friends {
                                        nodes {
                                            id
                                            ... @defer {
                                                name
                                            }
                                        }
                                    }
                                }
                            }
                        }");

            IResponseStream stream = Assert.IsType<DeferredQueryResult>(result);

            var memoryStream = new MemoryStream();
            var serializer = new MultiPartResponseStreamSerializer();

            // act
            await serializer.SerializeAsync(stream, memoryStream, CancellationToken.None);

            // assert
            memoryStream.Seek(0, SeekOrigin.Begin);
            new StreamReader(memoryStream).ReadToEnd().MatchSnapshot();
        }

        [Fact]
        public async Task Serialize_ResponseStream_Is_Null()
        {
            // arrange
            var serializer = new MultiPartResponseStreamSerializer();
           var stream = new Mock<Stream>();

            // act
            Task Action() =>
                serializer.SerializeAsync(null!, stream.Object, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(Action);
        }

        [Fact]
        public async Task Serialize_OutputStream_Is_Null()
        {
            // arrange
            var serializer = new MultiPartResponseStreamSerializer();
            var stream = new Mock<IResponseStream>();

            // act
            Task Action() =>
                serializer.SerializeAsync(stream.Object, null!, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(Action);
        }
    }
}
