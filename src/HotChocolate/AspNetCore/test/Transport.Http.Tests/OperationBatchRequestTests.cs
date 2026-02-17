using System.Text;
using System.Text.Json;

namespace HotChocolate.Transport.Http.Tests;

public class OperationBatchRequestTests
{
    [Fact]
    public async Task Should_WriteNullValues()
    {
        // arrange
        var request1 = new OperationRequest(
            null,
            "abc",
            "myOperation",
            variables: new Dictionary<string, object?>
            {
                ["abc"] = "def",
                ["hij"] = null
            });

        var request2 = new OperationRequest(
            query: """
                   query testQuery {
                     __typename
                   }
                   """,
            operationName: null,
            variables: new Dictionary<string, object?>
            {
                ["abc"] = 123,
                ["hij"] = null
            });

        var request = new OperationBatchRequest([request1, request2]);

        await using var memory = new MemoryStream();
        await using var writer = new Utf8JsonWriter(memory, new JsonWriterOptions { Indented = true });

        // act
        request.WriteTo(writer);
        await writer.FlushAsync();

        // assert
        var result = Encoding.UTF8.GetString(memory.ToArray());
        Assert.Equal(
            """
            [
              {
                "id": "abc",
                "operationName": "myOperation",
                "variables": {
                  "abc": "def",
                  "hij": null
                }
              },
              {
                "query": "query testQuery {\n  __typename\n}",
                "variables": {
                  "abc": 123,
                  "hij": null
                }
              }
            ]
            """,
            result);
    }
}
