using System.Text;
using System.Text.Json;

namespace HotChocolate.Transport.Http.Tests;

public class OperationRequestTests
{
    [Fact]
    public async Task Should_WriteNullValues()
    {
        // arrange
        var request = new OperationRequest(
            null,
            "abc",
            "myOperation",
            variables: new Dictionary<string, object?>()
            {
                ["abc"] = "def",
                ["hij"] = null,
            });

        using var memory = new MemoryStream();
        await using var writer = new Utf8JsonWriter(memory);

        // act
        request.WriteTo(writer);
        await writer.FlushAsync();

        // assert
        var result = Encoding.UTF8.GetString(memory.ToArray());
        Assert.Equal(
            """{"id":"abc","operationName":"myOperation","variables":{"abc":"def","hij":null}}""",
            result);
    }
}
