using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution;

public class JsonValueFormatterTests
{
    [Fact]
    public void WriteSByteAsNumber()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new JsonWriter(buffer, new JsonWriterOptions { SkipValidation = true });

        // act
        JsonValueFormatter.WriteValue(writer, (sbyte)2, new JsonSerializerOptions());

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("2", result);
    }
}
