using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Collections.Immutable;
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

    [Fact]
    public void WriteIncremental_Should_SerializeStreamItems_When_ItemHasErrors()
    {
        // arrange
        var item = new OperationResultData(
            new object(),
            isValueNull: false,
            new DictionaryJsonFormatter(),
            memoryHolder: null);
        var result = CreateIncrementalResult();
        result.Incremental =
        [
            new IncrementalListResult(
                123,
                item,
                ImmutableList.Create<IError>(ErrorBuilder.New().SetMessage("stream error").Build()))
        ];

        // act
        var serialized = SerializeIncremental(result);

        // assert
        serialized.MatchInlineSnapshot(
            """
            {
              "incremental": [
                {
                  "id": "123",
                  "errors": [
                    {
                      "message": "stream error"
                    }
                  ],
                  "items": [
                    {
                      "name": "Cato"
                    }
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public void WriteIncremental_Should_SerializeEntryExtensions_When_Present()
    {
        // arrange
        IReadOnlyDictionary<string, object?> extensions = new Dictionary<string, object?>
        {
            ["traceId"] = "abc"
        };
        var data = CreateData();
        var result = CreateIncrementalResult();
        result.Pending = [new PendingResult(1, Path.Root.Append("users"), Extensions: extensions)];
        result.Incremental = [new IncrementalObjectResult(1, data: data, extensions: extensions),
            new IncrementalListResult(2, CreateData(), extensions: extensions)];
        result.Completed = [new CompletedResult(1, Extensions: extensions)];

        // act
        var serialized = SerializeIncremental(result);

        // assert
        serialized.MatchInlineSnapshot(
            """
            {
              "pending": [
                {
                  "id": "1",
                  "path": [
                    "users"
                  ],
                  "extensions": {
                    "traceId": "abc"
                  }
                }
              ],
              "incremental": [
                {
                  "id": "1",
                  "extensions": {
                    "traceId": "abc"
                  },
                  "data": {
                    "name": "Cato"
                  }
                },
                {
                  "id": "2",
                  "extensions": {
                    "traceId": "abc"
                  },
                  "items": [
                    {
                      "name": "Cato"
                    }
                  ]
                }
              ],
              "completed": [
                {
                  "id": "1",
                  "extensions": {
                    "traceId": "abc"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public void WriteIncremental_Should_OmitEntryExtensions_When_Absent()
    {
        // arrange
        var data = CreateData();
        var result = CreateIncrementalResult();
        result.Pending = [new PendingResult(1, Path.Root.Append("users"))];
        result.Incremental = [new IncrementalObjectResult(1, data: data)];
        result.Completed = [new CompletedResult(1)];

        // act
        var serialized = SerializeIncremental(result);

        // assert
        serialized.MatchInlineSnapshot(
            """
            {
              "pending": [
                {
                  "id": "1",
                  "path": [
                    "users"
                  ]
                }
              ],
              "incremental": [
                {
                  "id": "1",
                  "data": {
                    "name": "Cato"
                  }
                }
              ],
              "completed": [
                {
                  "id": "1"
                }
              ]
            }
            """);
    }

    [Fact]
    public void WriteIncremental_Should_ThrowInvalidOperationException_When_IncrementalObjectDataIsMissing()
    {
        // arrange
        var result = CreateIncrementalResult();
        result.Incremental = [new IncrementalObjectResult(1)];

        // act
        void Serialize() => SerializeIncremental(result);

        // assert
        Assert.Throws<InvalidOperationException>(Serialize);
    }

    private static OperationResult CreateIncrementalResult()
        => new(ImmutableOrderedDictionary<string, object?>.Empty.Add("placeholder", true));

    private static OperationResultData CreateData()
        => new(new object(), isValueNull: false, new DictionaryJsonFormatter(), memoryHolder: null);

    private static string SerializeIncremental(OperationResult result)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new JsonWriter(buffer, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        JsonValueFormatter.WriteIncremental(writer, result, new JsonSerializerOptions());
        writer.WriteEndObject();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private sealed class DictionaryJsonFormatter : IRawJsonFormatter
    {
        public void WriteDataTo(JsonWriter writer)
            => JsonValueFormatter.WriteValue(
                writer,
                new Dictionary<string, object?> { ["name"] = "Cato" },
                new JsonSerializerOptions());
    }
}
