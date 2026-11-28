using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Text.Json;

public class ResultDocumentWriteTests
{
    [Theory]
    [InlineData("none", """{"first":"a\u003Cb"}""")]
    [InlineData("default", """{"first":"a\u003Cb"}""")]
    [InlineData("relaxed", """{"first":"a<b"}""")]
    [InlineData("strict", """{"\u0066\u0069\u0072\u0073\u0074":"\u0061\u003C\u0062"}""")]
    public void WriteDataTo_Should_ApplyTheEncoderToResponseNames_When_EncoderIsConfigured(
        string encoder,
        string expected)
    {
        // arrange
        var operation = CreateOperation(alias: null);
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        document.Data.GetProperty("first").SetStringValue("a<b");

        // act
        var json = Write(document, CreateOptions(encoder));

        // assert
        Assert.Equal(expected, json);
    }

    [Theory]
    [InlineData("_a1", """{"_a1":"value"}""")]
    [InlineData("a\"b", """{"a\"b":"value"}""")]
    [InlineData("a\\b", """{"a\\b":"value"}""")]
    [InlineData("a\nb", """{"a\nb":"value"}""")]
    [InlineData("a\u00e4b", "{\"a\u00e4b\":\"value\"}")]
    public void WriteDataTo_Should_EscapeTheResponseName_When_NameIsNotAPlainName(
        string alias,
        string expected)
    {
        // arrange
        var operation = CreateOperation(alias);
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        document.Data.GetProperty(alias).SetStringValue("value");

        // act
        var json = Write(document, CreateOptions("relaxed"));

        // assert
        Assert.Equal(expected, json);
    }

    [Fact]
    public void WriteDataTo_Should_WriteNamesOfPropertiesWithoutSelection_When_ObjectHasNoSelectionSet()
    {
        // arrange
        var operation = CreateOperation(alias: null);
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var first = document.Data.GetProperty("first");
        first.SetObjectValue(2);
        var properties = first.EnumerateObject().ToArray();
        properties[0].Value.SetPropertyName("plain");
        properties[0].Value.SetStringValue("a");
        properties[1].Value.SetPropertyName("quote\"name");
        properties[1].Value.SetStringValue("b");

        // act
        var json = Write(document, CreateOptions("relaxed"));

        // assert
        Assert.Equal("""{"first":{"plain":"a","quote\"name":"b"}}""", json);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WriteDataTo_Should_WriteStrings_When_ValuesCrossDataChunkBoundaries(bool isEncoded)
    {
        // arrange
        // 300 values of 97 bytes each cross the boundaries of the first data chunks at varying offsets
        var quote = isEncoded ? "\\u0022" : "\"";
        var operation = CreateOperation(alias: null);
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var first = document.Data.GetProperty("first");
        var values = new string[300];
        var expected = new string[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            var start = i.ToString("D3") + new string((char)('a' + (i % 26)), 40);
            var end = new string('z', 54 - quote.Length);
            values[i] = start + quote + end;
            expected[i] = start + (isEncoded ? quote : "\\\"") + end;
        }

        first.SetArrayValue(values.Length);

        for (var i = 0; i < values.Length; i++)
        {
            first[i].SetStringValue(values[i], isEncoded);
        }

        // act
        var json = Write(document, CreateOptions("relaxed"));

        // assert
        Assert.Equal("{\"first\":[\"" + string.Join("\",\"", expected) + "\"]}", json);
    }

    [Fact]
    public void WriteDataTo_Should_WriteNumbers_When_ValuesCrossDataChunkBoundaries()
    {
        // arrange
        // 300 values of 19 bytes each cross the boundaries of the first data chunks at varying offsets
        var operation = CreateOperation(alias: null);
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var first = document.Data.GetProperty("first");
        var values = new long[300];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = 1234567890123456000L + i;
        }

        first.SetArrayValue(values.Length);

        for (var i = 0; i < values.Length; i++)
        {
            first[i].SetNumberValue(values[i]);
        }

        // act
        var json = Write(document, CreateOptions("relaxed"));

        // assert
        Assert.Equal("{\"first\":[" + string.Join(",", values) + "]}", json);
    }

    private static string Write(ResultDocument document, JsonWriterOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        document.WriteDataTo(new JsonWriter(buffer, options));
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static JsonWriterOptions CreateOptions(string encoder)
        => encoder switch
        {
            "none" => new JsonWriterOptions(),
            "default" => new JsonWriterOptions { Encoder = JavaScriptEncoder.Default },
            "relaxed" => new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping },
            "strict" => new JsonWriterOptions { Encoder = JavaScriptEncoder.Create(new TextEncoderSettings()) },
            _ => throw new ArgumentOutOfRangeException(nameof(encoder))
        };

    private static Operation CreateOperation(string? alias)
    {
        var schema =
            SchemaBuilder.New()
                .AddDocumentFromString(
                    """
                    type Query {
                      first: String
                    }
                    """)
                .Use(_ => _)
                .Create();

        // The document is built from syntax nodes, because the parser only accepts GraphQL names as an alias.
        var field = new FieldNode(
            new NameNode("first"),
            alias is null ? null : new NameNode(alias),
            [],
            [],
            selectionSet: null);

        var operation = new OperationDefinitionNode(
            location: null,
            name: null,
            description: null,
            OperationType.Query,
            [],
            [],
            new SelectionSetNode([field]));

        return OperationCompiler.Compile("abc", new DocumentNode([operation]), schema);
    }
}
