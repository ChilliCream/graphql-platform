using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Serialization;

public class JsonArrayResponseStreamSerializerTests
{
    [Fact]
    public async Task Serialize_Response_Stream()
    {
        // arrange
        var result =
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
                                        name
                                    }
                                }
                            }
                        }
                    }");

        IResponseStream stream = Assert.IsType<ResponseStream>(result);

        var memoryStream = new MemoryStream();
        var serializer = new JsonArrayResponseStreamFormatter();

        // act
        await serializer.FormatAsync(stream, memoryStream, CancellationToken.None);

        // assert
        memoryStream.Seek(0, SeekOrigin.Begin);
        new StreamReader(memoryStream).ReadToEnd().MatchSnapshot();
    }

    [Fact]
    public async Task Serialize_ResponseStream_Is_Null()
    {
        // arrange
        var serializer = new JsonArrayResponseStreamFormatter();
        var stream = new Mock<Stream>();

        // act
        Task Action() =>
            serializer.FormatAsync(null!, stream.Object, CancellationToken.None);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }

    [Fact]
    public async Task Serialize_OutputStream_Is_Null()
    {
        // arrange
        var serializer = new JsonArrayResponseStreamFormatter();
        var stream = new Mock<IResponseStream>();

        // act
        Task Action() =>
            serializer.FormatAsync(stream.Object, null!, CancellationToken.None);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }
}
