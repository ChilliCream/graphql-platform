using System.Text;

namespace StrawberryShake.Transport.WebSockets;

public class SocketMessageWriterTest
{
    [Fact]
    public void WriteStartObject_EmptyBuffer_StartObject()
    {
        // arrange
        using var writer = new SocketMessageWriter();

        // act
        writer.WriteStartObject();

        // assert
        Encoding.UTF8.GetString(writer.Body.Span).MatchSnapshot();
    }

    [Fact]
    public void WriteEndObject_EmptyBuffer_EndObject()
    {
        // arrange
        using var writer = new SocketMessageWriter();

        // act
        writer.WriteEndObject();

        // assert
        Encoding.UTF8.GetString(writer.Body.Span).MatchSnapshot();
    }

    [Fact]
    public void Reset_ObjectInBuffer_EmptyBuffer()
    {
        // arrange
        using var writer = new SocketMessageWriter();
        writer.WriteStartObject();
        writer.WriteEndObject();

        // act
        writer.Reset();

        // assert
        Assert.True(writer.Body.IsEmpty);
    }
}
