using System.IO.Pipelines;
using HotChocolate.StarWars;
using HotChocolate.Transport.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Execution.Serialization;

public class MultiPartResponseStreamSerializerTests
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
                .ModifyOptions(
                    o =>
                    {
                        o.EnableDefer = true;
                        o.EnableStream = true;
                    })
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .ExecuteRequestAsync(
                    """
                    {
                        hero(episode: NEW_HOPE) {
                            id
                            ... @defer(label: "friends") {
                                friends {
                                    nodes {
                                        id
                                        name
                                    }
                                }
                            }
                        }
                    }
                    """);

        IResponseStream stream = Assert.IsType<ResponseStream>(result);

        var memoryStream = new MemoryStream();
        var serializer = new MultiPartResultFormatter();
        var writer = PipeWriter.Create(memoryStream, new StreamPipeWriterOptions(leaveOpen: true));

        // act
        await serializer.FormatAsync(stream, writer, CancellationToken.None);
        await writer.CompleteAsync();

        // assert
        memoryStream.Seek(0, SeekOrigin.Begin);
        (await new StreamReader(memoryStream).ReadToEndAsync()).MatchSnapshot();
    }

    [Fact]
    public async Task Serialize_ResponseStream_Is_Null()
    {
        // arrange
        var serializer = new MultiPartResultFormatter();
        var stream = new Mock<PipeWriter>();

        // act
        ValueTask Action() => serializer.FormatAsync(null!, stream.Object, CancellationToken.None);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Action());
    }

    [Fact]
    public async Task Serialize_OutputStream_Is_Null()
    {
        // arrange
        var serializer = new MultiPartResultFormatter();
        var stream = new Mock<IResponseStream>();

        // act
        ValueTask Action() => serializer.FormatAsync(stream.Object, null!, CancellationToken.None);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Action());
    }
}
