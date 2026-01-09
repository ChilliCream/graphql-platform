using System.Buffers;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Text.Json;

public class JsonWriterTests
{
    [Fact]
    public void WriteEmptyObject_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("{}", result);
    }

    [Fact]
    public void WriteEmptyObject_Indented()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("{}", result);
    }

    [Fact]
    public void WriteEmptyArray_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[]", result);
    }

    [Fact]
    public void WriteEmptyArray_Indented()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[]", result);
    }

    [Fact]
    public void WriteNullValue_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteNullValue();
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[null]", result);
    }

    [Fact]
    public void WriteBooleanTrue_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteBooleanValue(true);
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[true]", result);
    }

    [Fact]
    public void WriteBooleanFalse_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteBooleanValue(false);
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[false]", result);
    }

    [Fact]
    public void WriteStringValue_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStringValue("hello");
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[\"hello\"]", result);
    }

    [Fact]
    public void WriteStringValue_WithEscaping_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStringValue("hello\nworld");
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[\"hello\\nworld\"]", result);
    }

    [Fact]
    public void WriteStringValue_Utf8_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStringValue("hello"u8);
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[\"hello\"]", result);
    }

    [Fact]
    public void WriteNumberValue_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteNumberValue("42"u8);
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[42]", result);
    }

    [Fact]
    public void WritePropertyName_String_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WritePropertyName("name");
        writer.WriteStringValue("value");
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("{\"name\":\"value\"}", result);
    }

    [Fact]
    public void WritePropertyName_Utf8_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WritePropertyName("name"u8);
        writer.WriteStringValue("value");
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("{\"name\":\"value\"}", result);
    }

    [Fact]
    public void WriteMultipleProperties_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WritePropertyName("name");
        writer.WriteStringValue("John");
        writer.WritePropertyName("age");
        writer.WriteNumberValue("30"u8);
        writer.WritePropertyName("active");
        writer.WriteBooleanValue(true);
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("{\"name\":\"John\",\"age\":30,\"active\":true}", result);
    }

    [Fact]
    public void WriteMultipleProperties_Indented()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WritePropertyName("name");
        writer.WriteStringValue("John");
        writer.WritePropertyName("age");
        writer.WriteNumberValue("30"u8);
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        const string expected = """
            {
              "name": "John",
              "age": 30
            }
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteArrayWithMultipleValues_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStringValue("a");
        writer.WriteStringValue("b");
        writer.WriteStringValue("c");
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[\"a\",\"b\",\"c\"]", result);
    }

    [Fact]
    public void WriteArrayWithMultipleValues_Indented()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStringValue("a");
        writer.WriteStringValue("b");
        writer.WriteStringValue("c");
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        const string expected = """
            [
              "a",
              "b",
              "c"
            ]
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteNestedObject_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WritePropertyName("person");
        writer.WriteStartObject();
        writer.WritePropertyName("name");
        writer.WriteStringValue("John");
        writer.WriteEndObject();
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("{\"person\":{\"name\":\"John\"}}", result);
    }

    [Fact]
    public void WriteNestedObject_Indented()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WritePropertyName("person");
        writer.WriteStartObject();
        writer.WritePropertyName("name");
        writer.WriteStringValue("John");
        writer.WriteEndObject();
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        const string expected = """
            {
              "person": {
                "name": "John"
              }
            }
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteNestedArray_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStartArray();
        writer.WriteNumberValue("1"u8);
        writer.WriteNumberValue("2"u8);
        writer.WriteEndArray();
        writer.WriteStartArray();
        writer.WriteNumberValue("3"u8);
        writer.WriteNumberValue("4"u8);
        writer.WriteEndArray();
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[[1,2],[3,4]]", result);
    }

    [Fact]
    public void WriteNestedArray_Indented()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStartArray();
        writer.WriteNumberValue("1"u8);
        writer.WriteNumberValue("2"u8);
        writer.WriteEndArray();
        writer.WriteStartArray();
        writer.WriteNumberValue("3"u8);
        writer.WriteNumberValue("4"u8);
        writer.WriteEndArray();
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        const string expected = """
            [
              [
                1,
                2
              ],
              [
                3,
                4
              ]
            ]
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteArrayInObject_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WritePropertyName("items");
        writer.WriteStartArray();
        writer.WriteNumberValue("1"u8);
        writer.WriteNumberValue("2"u8);
        writer.WriteEndArray();
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("{\"items\":[1,2]}", result);
    }

    [Fact]
    public void WriteArrayInObject_Indented()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WritePropertyName("items");
        writer.WriteStartArray();
        writer.WriteNumberValue("1"u8);
        writer.WriteNumberValue("2"u8);
        writer.WriteEndArray();
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        const string expected = """
            {
              "items": [
                1,
                2
              ]
            }
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteObjectInArray_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("id");
        writer.WriteNumberValue("1"u8);
        writer.WriteEndObject();
        writer.WriteStartObject();
        writer.WritePropertyName("id");
        writer.WriteNumberValue("2"u8);
        writer.WriteEndObject();
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[{\"id\":1},{\"id\":2}]", result);
    }

    [Fact]
    public void WriteObjectInArray_Indented()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("id");
        writer.WriteNumberValue("1"u8);
        writer.WriteEndObject();
        writer.WriteStartObject();
        writer.WritePropertyName("id");
        writer.WriteNumberValue("2"u8);
        writer.WriteEndObject();
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        const string expected = """
            [
              {
                "id": 1
              },
              {
                "id": 2
              }
            ]
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CurrentDepth_TracksCorrectly()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act & assert
        Assert.Equal(0, writer.CurrentDepth);

        writer.WriteStartObject();
        Assert.Equal(1, writer.CurrentDepth);

        writer.WritePropertyName("nested");
        writer.WriteStartObject();
        Assert.Equal(2, writer.CurrentDepth);

        writer.WritePropertyName("array");
        writer.WriteStartArray();
        Assert.Equal(3, writer.CurrentDepth);

        writer.WriteEndArray();
        Assert.Equal(2, writer.CurrentDepth);

        writer.WriteEndObject();
        Assert.Equal(1, writer.CurrentDepth);

        writer.WriteEndObject();
        Assert.Equal(0, writer.CurrentDepth);
    }

    [Fact]
    public void WriteStringValue_Null_WritesNullLiteral()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStringValue(null);
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[null]", result);
    }

    [Fact]
    public void WriteStringValue_WithSpecialCharacters()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStringValue("tab\there");
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[\"tab\\there\"]", result);
    }

    [Fact]
    public void WriteStringValue_WithCarriageReturn()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStringValue("line1\r\nline2");
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[\"line1\\r\\nline2\"]", result);
    }

    [Fact]
    public void WriteStringValue_WithBackslash()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStringValue("path\\to\\file");
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[\"path\\\\to\\\\file\"]", result);
    }

    [Fact]
    public void WriteMixedArray_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStringValue("text");
        writer.WriteNumberValue("42"u8);
        writer.WriteBooleanValue(true);
        writer.WriteNullValue();
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[\"text\",42,true,null]", result);
    }

    [Fact]
    public void WriteMixedArray_Indented()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartArray();
        writer.WriteStringValue("text");
        writer.WriteNumberValue("42"u8);
        writer.WriteBooleanValue(true);
        writer.WriteNullValue();
        writer.WriteEndArray();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        const string expected = """
            [
              "text",
              42,
              true,
              null
            ]
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteComplexDocument_Minimized()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WritePropertyName("data");
        writer.WriteStartObject();
        writer.WritePropertyName("users");
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("id");
        writer.WriteNumberValue("1"u8);
        writer.WritePropertyName("name");
        writer.WriteStringValue("Alice");
        writer.WriteEndObject();
        writer.WriteStartObject();
        writer.WritePropertyName("id");
        writer.WriteNumberValue("2"u8);
        writer.WritePropertyName("name");
        writer.WriteStringValue("Bob");
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WritePropertyName("errors");
        writer.WriteNullValue();
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("{\"data\":{\"users\":[{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"name\":\"Bob\"}]},\"errors\":null}", result);
    }

    [Fact]
    public void WriteComplexDocument_Indented()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WritePropertyName("data");
        writer.WriteStartObject();
        writer.WritePropertyName("users");
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("id");
        writer.WriteNumberValue("1"u8);
        writer.WritePropertyName("name");
        writer.WriteStringValue("Alice");
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        const string expected = """
            {
              "data": {
                "users": [
                  {
                    "id": 1,
                    "name": "Alice"
                  }
                ]
              }
            }
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WritePropertyName_WithEscaping()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act
        writer.WriteStartObject();
        writer.WritePropertyName("special\nname");
        writer.WriteStringValue("value");
        writer.WriteEndObject();

        // assert
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("{\"special\\nname\":\"value\"}", result);
    }

    [Fact]
    public void Options_ReturnsConfiguredOptions()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = true, MaxDepth = 100 };
        var writer = new JsonWriter(buffer, options);

        // act
        var returnedOptions = writer.Options;

        // assert
        Assert.True(returnedOptions.Indented);
        Assert.Equal(100, returnedOptions.MaxDepth);
    }

    [Fact]
    public void WriteStringValue_Utf8_SkipEscaping_WritesPreEscapedContent()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act - write a pre-escaped value with quotes already included (like from JsonMarshal.GetRawUtf8Value)
        writer.WriteStartArray();
        writer.WriteStringValue("\"already\\nescaped\""u8, skipEscaping: true);
        writer.WriteEndArray();

        // assert - should not double-quote the value
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[\"already\\nescaped\"]", result);
    }

    [Fact]
    public void WriteStringValue_Utf8_WithoutSkipEscaping_EscapesContent()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act - write a value that contains a literal newline character (0x0A)
        writer.WriteStartArray();
        writer.WriteStringValue("line1\nline2"u8, skipEscaping: false);
        writer.WriteEndArray();

        // assert - the newline should be escaped to \n
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[\"line1\\nline2\"]", result);
    }

    [Fact]
    public void WriteStringValue_Utf8_SkipEscaping_Default_IsFalse()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { Indented = false, SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // act - call without skipEscaping parameter (should default to false and escape)
        writer.WriteStartArray();
        writer.WriteStringValue("line1\nline2"u8);
        writer.WriteEndArray();

        // assert - the newline should be escaped (proving default is false)
        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("[\"line1\\nline2\"]", result);
    }
}
