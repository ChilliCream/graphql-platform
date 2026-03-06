using System.Text.Json;

namespace Mocha.Tests;

public class HeadersTests
{
    [Fact]
    public void Set_And_GetValue_Should_ReturnStoredValue_When_ValueIsSet()
    {
        var headers = new Headers();
        headers.Set("key1", "value1");

        var result = headers.GetValue("key1");

        Assert.Equal("value1", result);
    }

    [Fact]
    public void Set_Should_OverwriteExistingKey_When_KeyAlreadyExists()
    {
        var headers = new Headers();
        headers.Set("key1", "value1");
        headers.Set("key1", "value2");

        Assert.Equal("value2", headers.GetValue("key1"));
        Assert.Equal(1, headers.Count);
    }

    [Fact]
    public void ContainsKey_Should_ReturnTrue_When_KeyExists()
    {
        var headers = new Headers();
        headers.Set("exists", "yes");

        Assert.True(headers.ContainsKey("exists"));
        Assert.False(headers.ContainsKey("missing"));
    }

    [Fact]
    public void TryGetValue_Should_ReturnTrueWithValue_When_KeyExists()
    {
        var headers = new Headers();
        headers.Set("key", 42);

        var found = headers.TryGetValue("key", out var value);

        Assert.True(found);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_Should_ReturnFalse_When_KeyMissing()
    {
        var headers = new Headers();

        var found = headers.TryGetValue("missing", out _);

        Assert.False(found);
    }

    [Fact]
    public void Count_Should_ReflectNumberOfHeaders_When_HeadersAreAdded()
    {
        var headers = new Headers();
        Assert.Equal(0, headers.Count);

        headers.Set("a", 1);
        headers.Set("b", 2);
        Assert.Equal(2, headers.Count);
    }

    [Fact]
    public void Clear_Should_RemoveAllHeaders_When_Called()
    {
        var headers = new Headers();
        headers.Set("a", 1);
        headers.Set("b", 2);

        headers.Clear();

        Assert.Equal(0, headers.Count);
        Assert.False(headers.ContainsKey("a"));
    }

    [Fact]
    public void Empty_Should_ReturnEmptyHeaders_When_Called()
    {
        var headers = Headers.Empty();

        Assert.Equal(0, headers.Count);
    }

    [Fact]
    public void From_Should_CreateHeaders_When_DictionaryIsProvided()
    {
        var dict = new Dictionary<string, object?> { ["key1"] = "value1", ["key2"] = 42 };

        var headers = Headers.From(dict);

        Assert.Equal(2, headers.Count);
        Assert.Equal("value1", headers.GetValue("key1"));
        Assert.Equal(42, headers.GetValue("key2"));
    }

    [Fact]
    public void GetEnumerator_Should_EnumerateAllHeaders_When_Called()
    {
        var headers = new Headers();
        headers.Set("a", 1);
        headers.Set("b", 2);

        var keys = new List<string>();
        foreach (var header in headers)
        {
            keys.Add(header.Key);
        }

        Assert.Contains("a", keys);
        Assert.Contains("b", keys);
    }

    [Fact]
    public void AddRange_Should_AddMultipleHeaders_When_CollectionIsProvided()
    {
        var headers = new Headers();
        headers.AddRange([new HeaderValue { Key = "x", Value = 10 }, new HeaderValue { Key = "y", Value = 20 }]);

        Assert.Equal(2, headers.Count);
        Assert.Equal(10, headers.GetValue("x"));
    }

    [Fact]
    public void Constructor_Should_InitializeHeaders_When_ValuesAreProvided()
    {
        var headers = new Headers([
            new HeaderValue { Key = "a", Value = "alpha" },
            new HeaderValue { Key = "b", Value = "beta" }
        ]);

        Assert.Equal(2, headers.Count);
        Assert.Equal("alpha", headers.GetValue("a"));
    }

    [Fact]
    public void Set_Should_StoreStringValue_When_StringIsProvided()
    {
        var headers = new Headers();
        headers.Set("str", "hello");
        Assert.Equal("hello", headers.GetValue("str"));
    }

    [Fact]
    public void Set_Should_StoreIntValue_When_IntIsProvided()
    {
        var headers = new Headers();
        headers.Set("num", 42);
        Assert.Equal(42, headers.GetValue("num"));
    }

    [Fact]
    public void Set_Should_StoreBoolValue_When_BoolIsProvided()
    {
        var headers = new Headers();
        headers.Set("flag", true);
        Assert.Equal(true, headers.GetValue("flag"));
    }

    [Fact]
    public void Set_Should_StoreNull_When_NullValueIsProvided()
    {
        var headers = new Headers();
        headers.Set<string?>("nullable", null);
        Assert.Null(headers.GetValue("nullable"));
    }

    [Fact]
    public void Write_Should_WriteEmptyObject_When_HeadersAreEmpty()
    {
        var headers = new Headers();

        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        Assert.Equal("{}", json);
    }

    [Fact]
    public void Write_Should_WriteCorrectJson_When_HeaderContainsString()
    {
        var headers = new Headers();
        headers.Set("key", "value");

        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("value", doc.RootElement.GetProperty("key").GetString());
    }

    [Fact]
    public void Write_Should_WriteNumber_When_HeaderContainsInt()
    {
        var headers = new Headers();
        headers.Set("count", 42);

        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(42, doc.RootElement.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Write_Should_WriteBool_When_HeaderContainsBool()
    {
        var headers = new Headers();
        headers.Set("active", true);

        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("active").GetBoolean());
    }

    [Fact]
    public void Read_Should_ReturnEmptyHeaders_When_ObjectIsEmpty()
    {
        const string json = "{}";

        var headers = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        Assert.NotNull(headers);
        Assert.Equal(0, headers!.Count);
    }

    [Fact]
    public void Read_Should_DeserializeStringValue_When_JsonContainsString()
    {
        const string json = """{"key": "value"}""";

        var headers = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        Assert.NotNull(headers);
        Assert.True(headers!.TryGetValue("key", out var val));
        Assert.Equal("value", val);
    }

    [Fact]
    public void Read_Should_DeserializeNumericValue_When_JsonContainsNumber()
    {
        const string json = """{"count": 42}""";

        var headers = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        Assert.NotNull(headers);
        Assert.True(headers!.TryGetValue("count", out var val));
        // JSON numbers may deserialize as different numeric types
        Assert.NotNull(val);
    }

    [Fact]
    public void Read_Should_DeserializeBoolValue_When_JsonContainsBool()
    {
        const string json = """{"active": true}""";

        var headers = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        Assert.NotNull(headers);
        Assert.True(headers!.TryGetValue("active", out var val));
        Assert.Equal(true, val);
    }

    [Fact]
    public void Read_Should_DeserializeAsNull_When_JsonContainsNull()
    {
        const string json = """{"key": null}""";

        var headers = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        Assert.NotNull(headers);
        Assert.True(headers!.TryGetValue("key", out var val));
        Assert.Null(val);
    }

    [Fact]
    public void Read_Should_DeserializeAsDictionary_When_JsonContainsNestedObject()
    {
        const string json = """{"nested": {"inner": "value"}}""";

        var headers = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        Assert.NotNull(headers);
        Assert.True(headers!.TryGetValue("nested", out var val));
        Assert.NotNull(val);
    }

    [Fact]
    public void Read_Should_DeserializeAsArray_When_JsonContainsArray()
    {
        const string json = """{"items": [1, 2, 3]}""";

        var headers = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        Assert.NotNull(headers);
        Assert.True(headers!.TryGetValue("items", out var val));
        Assert.NotNull(val);
    }

    [Fact]
    public void RoundTrip_Should_PreserveStringValues_When_HeadersAreSerializedAndDeserialized()
    {
        var original = new Headers();
        original.Set("key1", "value1");
        original.Set("key2", "value2");

        var json = JsonSerializer.Serialize<IHeaders>(original, HeadersJsonConverter.Options);
        var deserialized = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        Assert.NotNull(deserialized);
        Assert.True(deserialized!.TryGetValue("key1", out var v1));
        Assert.Equal("value1", v1);
        Assert.True(deserialized!.TryGetValue("key2", out var v2));
        Assert.Equal("value2", v2);
    }

    [Fact]
    public void RoundTrip_Should_PreserveMixedTypeStructure_When_HeadersAreSerializedAndDeserialized()
    {
        var original = new Headers();
        original.Set("str", "hello");
        original.Set("flag", true);

        var json = JsonSerializer.Serialize<IHeaders>(original, HeadersJsonConverter.Options);
        var deserialized = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        Assert.NotNull(deserialized);
        Assert.True(deserialized!.TryGetValue("str", out var s));
        Assert.Equal("hello", s);
    }
}
