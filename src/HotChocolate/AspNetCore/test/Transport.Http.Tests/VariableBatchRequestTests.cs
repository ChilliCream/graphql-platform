using System.Text;
using System.Text.Json;
using CookieCrumble;

namespace HotChocolate.Transport.Http.Tests;

public class VariableBatchRequestTests
{
    [Fact]
    public async Task Should_WriteNullValues()
    {
        // arrange
        var request = new VariableBatchRequest(
            null,
            "abc",
            "myOperation",
            variables: 
            [
                new Dictionary<string, object?>
                {
                    ["abc"] = "def",
                    ["hij"] = null,
                },
                new Dictionary<string, object?>
                {
                    ["abc"] = "xyz",
                    ["hij"] = null,
                },
            ]);

        using var memory = new MemoryStream();
        await using var writer = new Utf8JsonWriter(memory);

        // act
        request.WriteTo(writer);
        await writer.FlushAsync();

        // assert
        var result = Encoding.UTF8.GetString(memory.ToArray());
        result.MatchInlineSnapshot(
            """{"id":"abc","operationName":"myOperation","variables":[{"abc":"def","hij":null},{"abc":"xyz","hij":null}]}""");
    }
}