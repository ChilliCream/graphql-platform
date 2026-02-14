using System.Buffers;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Text.Json;

public class JsonWriterNullIgnoreTests
{
    [Fact]
    public void Default_NullIgnoreCondition_IsNone()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { SkipValidation = true };
        var writer = new JsonWriter(buffer, options);

        // assert
        Assert.Equal(JsonNullIgnoreCondition.None, writer.NullIgnoreCondition);
        Assert.False(writer.IgnoreNullFields);
        Assert.False(writer.IgnoreNullListElements);
    }

    [Fact]
    public void NullIgnoreCondition_Fields_SetsIgnoreNullFields()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { SkipValidation = true };
        var writer = new JsonWriter(buffer, options, JsonNullIgnoreCondition.Fields);

        // assert
        Assert.Equal(JsonNullIgnoreCondition.Fields, writer.NullIgnoreCondition);
        Assert.True(writer.IgnoreNullFields);
        Assert.False(writer.IgnoreNullListElements);
    }

    [Fact]
    public void NullIgnoreCondition_Lists_SetsIgnoreNullListElements()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { SkipValidation = true };
        var writer = new JsonWriter(buffer, options, JsonNullIgnoreCondition.Lists);

        // assert
        Assert.Equal(JsonNullIgnoreCondition.Lists, writer.NullIgnoreCondition);
        Assert.False(writer.IgnoreNullFields);
        Assert.True(writer.IgnoreNullListElements);
    }

    [Fact]
    public void NullIgnoreCondition_FieldsAndLists_SetsBoth()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { SkipValidation = true };
        var writer = new JsonWriter(buffer, options, JsonNullIgnoreCondition.FieldsAndLists);

        // assert
        Assert.Equal(JsonNullIgnoreCondition.FieldsAndLists, writer.NullIgnoreCondition);
        Assert.True(writer.IgnoreNullFields);
        Assert.True(writer.IgnoreNullListElements);
    }

    [Fact]
    public void IgnoreNullFields_OmitsNullFieldValues()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("name");
            writer.WriteStringValue("Alice");
            writer.WritePropertyName("age");
            writer.WriteNullValue();
            writer.WritePropertyName("email");
            writer.WriteStringValue("alice@example.com");
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"name":"Alice","email":"alice@example.com"}""", json);
    }

    [Fact]
    public void IgnoreNullFields_KeepsNonNullFields()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("a");
            writer.WriteNumberValue(1);
            writer.WritePropertyName("b");
            writer.WriteBooleanValue(true);
            writer.WritePropertyName("c");
            writer.WriteStringValue("hello");
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"a":1,"b":true,"c":"hello"}""", json);
    }

    [Fact]
    public void IgnoreNullFields_AllFieldsNull_WritesEmptyObject()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("a");
            writer.WriteNullValue();
            writer.WritePropertyName("b");
            writer.WriteNullValue();
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("{}", json);
    }

    [Fact]
    public void IgnoreNullFields_NullStringValue_OmitsField()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("name");
            writer.WriteStringValue((string?)null);
            writer.WritePropertyName("value");
            writer.WriteNumberValue(42);
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"value":42}""", json);
    }

    [Fact]
    public void IgnoreNullFields_NestedObject_OmitsNullFieldsAtAllLevels()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("outer");
            writer.WriteStartObject();
            writer.WritePropertyName("inner");
            writer.WriteStringValue("value");
            writer.WritePropertyName("nullField");
            writer.WriteNullValue();
            writer.WriteEndObject();
            writer.WritePropertyName("topNull");
            writer.WriteNullValue();
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"outer":{"inner":"value"}}""", json);
    }

    [Fact]
    public void IgnoreNullFields_PropertyFollowedByStartObject_FlushesPropertyName()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("data");
            writer.WriteStartObject();
            writer.WritePropertyName("id");
            writer.WriteNumberValue(1);
            writer.WriteEndObject();
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"data":{"id":1}}""", json);
    }

    [Fact]
    public void IgnoreNullFields_PropertyFollowedByStartArray_FlushesPropertyName()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("items");
            writer.WriteStartArray();
            writer.WriteNumberValue(1);
            writer.WriteNumberValue(2);
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"items":[1,2]}""", json);
    }

    [Fact]
    public void IgnoreNullFields_PropertyFollowedByRawValue_FlushesPropertyName()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("raw");
            writer.WriteRawValue("true"u8);
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"raw":true}""", json);
    }

    [Fact]
    public void IgnoreNullListElements_OmitsNullsFromArray()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Lists, writer =>
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(1);
            writer.WriteNullValue();
            writer.WriteNumberValue(2);
            writer.WriteNullValue();
            writer.WriteNumberValue(3);
            writer.WriteEndArray();
        });

        // assert
        Assert.Equal("[1,2,3]", json);
    }

    [Fact]
    public void IgnoreNullListElements_AllNulls_WritesEmptyArray()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Lists, writer =>
        {
            writer.WriteStartArray();
            writer.WriteNullValue();
            writer.WriteNullValue();
            writer.WriteEndArray();
        });

        // assert
        Assert.Equal("[]", json);
    }

    [Fact]
    public void IgnoreNullListElements_NestedArray_OmitsNullsAtAllLevels()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Lists, writer =>
        {
            writer.WriteStartArray();
            writer.WriteStartArray();
            writer.WriteNullValue();
            writer.WriteNumberValue(1);
            writer.WriteEndArray();
            writer.WriteNullValue();
            writer.WriteEndArray();
        });

        // assert
        Assert.Equal("[[1]]", json);
    }

    [Fact]
    public void IgnoreNullListElements_DoesNotAffectObjectFields()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Lists, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("field");
            writer.WriteNullValue();
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"field":null}""", json);
    }

    [Fact]
    public void IgnoreNullFields_DoesNotAffectArrayElements()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartArray();
            writer.WriteNullValue();
            writer.WriteNumberValue(1);
            writer.WriteEndArray();
        });

        // assert
        Assert.Equal("[null,1]", json);
    }

    [Fact]
    public void FieldsAndLists_OmitsBoth()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.FieldsAndLists, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("name");
            writer.WriteNullValue();
            writer.WritePropertyName("items");
            writer.WriteStartArray();
            writer.WriteNullValue();
            writer.WriteNumberValue(1);
            writer.WriteNullValue();
            writer.WriteEndArray();
            writer.WritePropertyName("active");
            writer.WriteBooleanValue(true);
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"items":[1],"active":true}""", json);
    }

    [Fact]
    public void None_WritesAllNulls()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.None, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("field");
            writer.WriteNullValue();
            writer.WritePropertyName("items");
            writer.WriteStartArray();
            writer.WriteNullValue();
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"field":null,"items":[null]}""", json);
    }

    [Fact]
    public void IgnoreNullFields_ObjectInsideArray_OmitsNullFieldsInNestedObject()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WritePropertyName("id");
            writer.WriteNumberValue(1);
            writer.WritePropertyName("name");
            writer.WriteNullValue();
            writer.WriteEndObject();
            writer.WriteEndArray();
        });

        // assert
        Assert.Equal("""[{"id":1}]""", json);
    }

    [Fact]
    public void IgnoreNullListElements_ArrayInsideObject_OmitsNullElements()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Lists, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("items");
            writer.WriteStartArray();
            writer.WriteNullValue();
            writer.WriteStringValue("a");
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"items":["a"]}""", json);
    }

    [Fact]
    public void IgnoreNullFields_NullFieldBeforeLastField_CorrectCommas()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("a");
            writer.WriteNumberValue(1);
            writer.WritePropertyName("b");
            writer.WriteNullValue();
            writer.WritePropertyName("c");
            writer.WriteNumberValue(3);
            writer.WriteEndObject();
        });

        // assert - verify no trailing comma after "a":1
        Assert.Equal("""{"a":1,"c":3}""", json);
    }

    [Fact]
    public void IgnoreNullFields_FirstFieldNull_CorrectOutput()
    {
        // arrange & act
        var json = WriteJson(JsonNullIgnoreCondition.Fields, writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("first");
            writer.WriteNullValue();
            writer.WritePropertyName("second");
            writer.WriteNumberValue(2);
            writer.WriteEndObject();
        });

        // assert
        Assert.Equal("""{"second":2}""", json);
    }

    private static string WriteJson(JsonNullIgnoreCondition condition, Action<JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var options = new JsonWriterOptions { SkipValidation = true };
        var writer = new JsonWriter(buffer, options, condition);

        write(writer);

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
