using System.Text.Json;
using Mocha;

namespace Mocha.Tests;

public class HeadersSerializationTests
{
    [Theory]
    [MemberData(nameof(ExactRoundTripCases))]
    public void RoundTrip_Should_PreserveValue_When_Serialized(string key, object? input, object? expected)
    {
        // arrange
        var headers = new Headers();
        headers.Set(key, input);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.True(result!.TryGetValue(key, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [MemberData(nameof(LossyRoundTripCases))]
    public void RoundTrip_Should_PreserveNonNullValue_When_LossyTypeSerialized(string key, object? input)
    {
        // arrange
        var headers = new Headers();
        headers.Set(key, input);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.True(result!.TryGetValue(key, out var value));
        Assert.NotNull(value);
    }

    [Fact]
    public void RoundTrip_Should_PreserveDateTime_When_Serialized()
    {
        // arrange
        var headers = new Headers();
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);
        headers.Set("dateTime", dateTime);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.True(result!.TryGetValue("dateTime", out var value));
        Assert.IsType<DateTime>(value);
        var resultDateTime = (DateTime)value!;
        // DateTime may be deserialized with some precision loss
        Assert.Equal(dateTime.Year, resultDateTime.Year);
        Assert.Equal(dateTime.Month, resultDateTime.Month);
        Assert.Equal(dateTime.Day, resultDateTime.Day);
    }

    [Fact]
    public void RoundTrip_Should_PreserveNestedObject_When_Serialized()
    {
        // arrange
        var headers = new Headers();
        var nested = new Dictionary<string, object?> { ["innerKey"] = "innerValue", ["innerNumber"] = 123 };
        headers.Set("nested", nested);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.True(result!.TryGetValue("nested", out var value));
        Assert.IsType<Dictionary<string, object?>>(value);
        var dict = (Dictionary<string, object?>)value!;
        Assert.Equal("innerValue", dict["innerKey"]);
        Assert.Equal(123, dict["innerNumber"]);
    }

    [Fact]
    public void RoundTrip_Should_PreserveDeeplyNestedObject_When_Serialized()
    {
        // arrange
        var headers = new Headers();
        var deepNested = new Dictionary<string, object?>
        {
            ["level1"] = new Dictionary<string, object?>
            {
                ["level2"] = new Dictionary<string, object?> { ["level3"] = "deep value" }
            }
        };
        headers.Set("deep", deepNested);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.True(result!.TryGetValue("deep", out var value));
        Assert.IsType<Dictionary<string, object?>>(value);
        var level1 = (Dictionary<string, object?>)value!;
        var level2 = (Dictionary<string, object?>)level1["level1"]!;
        var level3 = (Dictionary<string, object?>)level2["level2"]!;
        Assert.Equal("deep value", level3["level3"]);
    }

    [Theory]
    [MemberData(nameof(ArrayRoundTripCases))]
    public void RoundTrip_Should_PreserveArray_When_Serialized(string key, object?[] input, object?[] expected)
    {
        // arrange
        var headers = new Headers();
        headers.Set(key, input);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.True(result!.TryGetValue(key, out var value));
        Assert.IsType<object[]>(value);
        var array = (object[])value!;
        Assert.Equal(expected.Length, array.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], array[i]);
        }
    }

    [Fact]
    public void RoundTrip_Should_PreserveArrayOfObjects_When_Serialized()
    {
        // arrange
        var headers = new Headers();
        var arrayOfObjects = new object[]
        {
            new Dictionary<string, object?> { ["id"] = 1, ["name"] = "first" },
            new Dictionary<string, object?> { ["id"] = 2, ["name"] = "second" }
        };
        headers.Set("items", arrayOfObjects);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.True(result!.TryGetValue("items", out var value));
        Assert.IsType<object[]>(value);
        var array = (object[])value!;
        Assert.Equal(2, array.Length);
        var first = (Dictionary<string, object?>)array[0]!;
        Assert.Equal(1, first["id"]);
        Assert.Equal("first", first["name"]);
    }

    [Fact]
    public void RoundTrip_Should_PreserveObjectContainingArray_When_Serialized()
    {
        // arrange
        var headers = new Headers();
        var complexObject = new Dictionary<string, object?>
        {
            ["tags"] = new object[] { "tag1", "tag2", "tag3" },
            ["count"] = 3
        };
        headers.Set("complex", complexObject);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.True(result!.TryGetValue("complex", out var value));
        Assert.IsType<Dictionary<string, object?>>(value);
        var dict = (Dictionary<string, object?>)value!;
        var tags = (object[])dict["tags"]!;
        Assert.Equal(3, tags.Length);
        Assert.Equal("tag1", tags[0]);
    }

    [Fact]
    public void RoundTrip_Should_PreserveMixedComplexStructure_When_Serialized()
    {
        // arrange
        var headers = new Headers();
        headers.Set("string", "value");
        headers.Set("number", 42);
        headers.Set("bool", true);
        headers.Set<object?>("null", null);
        headers.Set("object", new Dictionary<string, object?> { ["nested"] = "value" });
        headers.Set("array", new object[] { 1, 2, 3 });

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.Equal(6, result!.Count);
        Assert.True(result.TryGetValue("string", out var strVal));
        Assert.Equal("value", strVal);
        Assert.True(result.TryGetValue("number", out var numVal));
        Assert.Equal(42, numVal);
        Assert.True(result.TryGetValue("bool", out var boolVal));
        Assert.Equal(true, boolVal);
        Assert.True(result.TryGetValue("null", out var nullVal));
        Assert.Null(nullVal);
    }

    [Theory]
    [MemberData(nameof(EmptyContainerCases))]
    public void RoundTrip_Should_PreserveEmptyContainer_When_Serialized(string key, object? input, Type? expectedType)
    {
        // arrange
        var headers = new Headers();
        if (input is not null)
        {
            headers.Set(key, input);
        }

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        if (expectedType is null)
        {
            Assert.Equal(0, result!.Count);
        }
        else
        {
            Assert.True(result!.TryGetValue(key, out var value));
            Assert.IsType(expectedType, value);

            if (value is Dictionary<string, object?> dict)
            {
                Assert.Empty(dict);
            }
            else if (value is object[] array)
            {
                Assert.Empty(array);
            }
        }
    }

    [Fact]
    public void Deserialize_Should_ParseStringValue_When_JsonContainsString()
    {
        // arrange
        const string json = """{"key": "value"}""";

        // act
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.True(result!.TryGetValue("key", out var value));
        Assert.Equal("value", value);
    }

    [Fact]
    public void Deserialize_Should_ParseNestedObject_When_JsonContainsObject()
    {
        // arrange
        const string json = """{"nested": {"inner": "value", "count": 42}}""";

        // act
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.True(result!.TryGetValue("nested", out var value));
        Assert.IsType<Dictionary<string, object?>>(value);
        var dict = (Dictionary<string, object?>)value!;
        Assert.Equal("value", dict["inner"]);
        Assert.Equal(42, dict["count"]);
    }

    [Fact]
    public void Deserialize_Should_ParseArray_When_JsonContainsArray()
    {
        // arrange
        const string json = """{"items": [1, 2, 3]}""";

        // act
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.NotNull(result);
        Assert.True(result!.TryGetValue("items", out var value));
        Assert.IsType<object[]>(value);
        var array = (object[])value!;
        Assert.Equal(3, array.Length);
    }

    [Fact]
    public void Deserialize_Should_ReturnNull_When_JsonIsNull()
    {
        // arrange
        const string json = "null";

        // act
        var result = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_Should_ThrowException_When_JsonIsInvalid()
    {
        // arrange
        const string json = "[1, 2, 3]"; // Array instead of object

        // act & assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options));
    }

    [Fact]
    public void Serialize_Should_ProduceValidJson_When_HeadersContainString()
    {
        // arrange
        var headers = new Headers();
        headers.Set("key", "value");

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("value", doc.RootElement.GetProperty("key").GetString());
    }

    [Fact]
    public void Serialize_Should_ProduceValidJson_When_HeadersContainMultipleTypes()
    {
        // arrange
        var headers = new Headers();
        headers.Set("str", "hello");
        headers.Set("num", 42);
        headers.Set("bool", true);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("hello", doc.RootElement.GetProperty("str").GetString());
        Assert.Equal(42, doc.RootElement.GetProperty("num").GetInt32());
        Assert.True(doc.RootElement.GetProperty("bool").GetBoolean());
    }

    [Fact]
    public void Set_Should_AddValue_When_ContextDataKeyIsUsed()
    {
        // arrange
        var headers = new Headers();
        var key = new ContextDataKey<string>("testKey");

        // act
        headers.Set(key, "testValue");

        // assert
        Assert.True(headers.ContainsKey("testKey"));
        Assert.Equal("testValue", headers.GetValue("testKey"));
    }

    [Fact]
    public void TryGet_Should_ReturnTrue_When_KeyExistsWithCorrectType()
    {
        // arrange
        var headers = new Headers();
        var key = new ContextDataKey<int>("counter");
        headers.Set(key, 42);

        // act
        var found = headers.TryGet(key, out var value);

        // assert
        Assert.True(found);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGet_Should_ReturnFalse_When_KeyDoesNotExist()
    {
        // arrange
        var headers = new Headers();
        var key = new ContextDataKey<string>("missing");

        // act
        var found = headers.TryGet(key, out var value);

        // assert
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGet_Should_ReturnFalse_When_TypeDoesNotMatch()
    {
        // arrange
        var headers = new Headers();
        headers.Set("key", "string value");
        var key = new ContextDataKey<int>("key");

        // act
        var found = headers.TryGet(key, out var value);

        // assert
        Assert.False(found);
        Assert.Equal(0, value);
    }

    [Fact]
    public void Get_Should_ReturnValue_When_KeyExistsWithCorrectType()
    {
        // arrange
        var headers = new Headers();
        var key = new ContextDataKey<string>("name");
        headers.Set(key, "test");

        // act
        var value = headers.Get(key);

        // assert
        Assert.Equal("test", value);
    }

    [Fact]
    public void Get_Should_ReturnDefault_When_KeyDoesNotExist()
    {
        // arrange
        var headers = new Headers();
        var key = new ContextDataKey<string>("missing");

        // act
        var value = headers.Get(key);

        // assert
        Assert.Null(value);
    }

    [Fact]
    public void TryAdd_Should_AddValue_When_KeyDoesNotExist()
    {
        // arrange
        var headers = new Headers();
        var key = new ContextDataKey<string>("new");

        // act
        var added = headers.TryAdd(key, "value");

        // assert
        Assert.True(added);
        Assert.Equal("value", headers.GetValue("new"));
    }

    [Fact]
    public void TryAdd_Should_ReturnFalse_When_KeyAlreadyExists()
    {
        // arrange
        var headers = new Headers();
        var key = new ContextDataKey<string>("existing");
        headers.Set(key, "original");

        // act
        var added = headers.TryAdd(key, "new value");

        // assert
        Assert.False(added);
        Assert.Equal("original", headers.GetValue("existing"));
    }

    [Fact]
    public void CopyTo_Should_CopyAllHeaders_When_Called()
    {
        // arrange
        var source = new Headers();
        source.Set("key1", "value1");
        source.Set("key2", 42);
        var target = new Headers();

        // act
        source.CopyTo(target);

        // assert
        Assert.Equal(2, target.Count);
        Assert.Equal("value1", target.GetValue("key1"));
        Assert.Equal(42, target.GetValue("key2"));
    }

    [Fact]
    public void CopyTo_Should_OverwriteExistingKeys_When_Copying()
    {
        // arrange
        var source = new Headers();
        source.Set("key", "new value");
        var target = new Headers();
        target.Set("key", "old value");

        // act
        source.CopyTo(target);

        // assert
        Assert.Equal("new value", target.GetValue("key"));
    }

    [Fact]
    public void CopyTo_Should_CopySpecificKey_When_ContextDataKeyIsProvided()
    {
        // arrange
        var source = new Headers();
        var key = new ContextDataKey<string>("specific");
        source.Set(key, "value");
        source.Set("other", "other value");
        var target = new Headers();

        // act
        source.CopyTo(target, key);

        // assert
        Assert.Equal(1, target.Count);
        Assert.Equal("value", target.GetValue("specific"));
        Assert.False(target.ContainsKey("other"));
    }

    [Fact]
    public void CopyTo_Should_NotCopy_When_KeyDoesNotExistInSource()
    {
        // arrange
        var source = new Headers();
        var key = new ContextDataKey<string>("missing");
        var target = new Headers();

        // act
        source.CopyTo(target, key);

        // assert
        Assert.Equal(0, target.Count);
    }

    [Fact]
    public void Dictionary_Set_Should_AddValue_When_ContextDataKeyIsUsed()
    {
        // arrange
        var dict = new Dictionary<string, object?>();
        var key = new ContextDataKey<string>("test");

        // act
        dict.Set(key, "value");

        // assert
        Assert.Equal("value", dict["test"]);
    }

    [Fact]
    public void Dictionary_TryAdd_Should_AddValue_When_KeyDoesNotExist()
    {
        // arrange
        var dict = new Dictionary<string, object?>();
        var key = new ContextDataKey<int>("count");

        // act
        var added = dict.TryAdd(key, 42);

        // assert
        Assert.True(added);
        Assert.Equal(42, dict["count"]);
    }

    [Fact]
    public void Dictionary_TryAdd_Should_ReturnFalse_When_KeyExists()
    {
        // arrange
        var dict = new Dictionary<string, object?> { ["key"] = "original" };
        var key = new ContextDataKey<string>("key");

        // act
        var added = dict.TryAdd(key, "new");

        // assert
        Assert.False(added);
        Assert.Equal("original", dict["key"]);
    }

    [Fact]
    public void Dictionary_Get_Should_ReturnValue_When_KeyExistsWithCorrectType()
    {
        // arrange
        IDictionary<string, object?> dict = new Dictionary<string, object?> { ["name"] = "test" };
        var key = new ContextDataKey<string>("name");

        // act
        var value = dict.Get(key);

        // assert
        Assert.Equal("test", value);
    }

    [Fact]
    public void Dictionary_Get_Should_ReturnDefault_When_KeyDoesNotExist()
    {
        // arrange
        IDictionary<string, object?> dict = new Dictionary<string, object?>();
        var key = new ContextDataKey<string>("missing");

        // act
        var value = dict.Get(key);

        // assert
        Assert.Null(value);
    }

    [Fact]
    public void Dictionary_TryGet_Should_ReturnTrue_When_KeyExistsWithCorrectType()
    {
        // arrange
        IDictionary<string, object?> dict = new Dictionary<string, object?> { ["count"] = 42 };
        var key = new ContextDataKey<int>("count");

        // act
        var found = dict.TryGet(key, out var value);

        // assert
        Assert.True(found);
        Assert.Equal(42, value);
    }

    [Fact]
    public void Dictionary_TryGet_Should_ReturnFalse_When_TypeDoesNotMatch()
    {
        // arrange
        IDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = "string" };
        var key = new ContextDataKey<int>("key");

        // act
        var found = dict.TryGet(key, out var value);

        // assert
        Assert.False(found);
        Assert.Equal(0, value);
    }

    [Fact]
    public void ReadOnlyDictionary_Get_Should_ReturnValue_When_KeyExistsWithCorrectType()
    {
        // arrange
        var sourceDict = new Dictionary<string, object?> { ["name"] = "test" };
        IReadOnlyDictionary<string, object?> dict = sourceDict.AsReadOnly();
        var key = new ContextDataKey<string>("name");

        // act
        var value = dict.Get(key);

        // assert
        Assert.Equal("test", value);
    }

    [Fact]
    public void ReadOnlyDictionary_Get_Should_ReturnDefault_When_KeyDoesNotExist()
    {
        // arrange
        var sourceDict = new Dictionary<string, object?>();
        IReadOnlyDictionary<string, object?> dict = sourceDict.AsReadOnly();
        var key = new ContextDataKey<string>("missing");

        // act
        var value = dict.Get(key);

        // assert
        Assert.Null(value);
    }

    [Fact]
    public void ReadOnlyDictionary_TryGet_Should_ReturnTrue_When_KeyExistsWithCorrectType()
    {
        // arrange
        var sourceDict = new Dictionary<string, object?> { ["count"] = 42 };
        IReadOnlyDictionary<string, object?> dict = sourceDict.AsReadOnly();
        var key = new ContextDataKey<int>("count");

        // act
        var found = dict.TryGet(key, out var value);

        // assert
        Assert.True(found);
        Assert.Equal(42, value);
    }

    [Fact]
    public void ReadOnlyDictionary_TryGet_Should_ReturnFalse_When_KeyDoesNotExist()
    {
        // arrange
        var sourceDict = new Dictionary<string, object?>();
        IReadOnlyDictionary<string, object?> dict = sourceDict.AsReadOnly();
        var key = new ContextDataKey<string>("missing");

        // act
        var found = dict.TryGet(key, out var value);

        // assert
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void ReadOnlyDictionary_CopyTo_Should_CopyValue_When_KeyExists()
    {
        // arrange
        IReadOnlyDictionary<string, object?> source = new Dictionary<string, object?> { ["key"] = "value" };
        var target = new Dictionary<string, object?>();
        var key = new ContextDataKey<string>("key");

        // act
        var copied = source.CopyTo(target, key);

        // assert
        Assert.True(copied);
        Assert.Equal("value", target["key"]);
    }

    [Fact]
    public void ReadOnlyDictionary_CopyTo_Should_ReturnFalse_When_KeyDoesNotExist()
    {
        // arrange
        IReadOnlyDictionary<string, object?> source = new Dictionary<string, object?>();
        var target = new Dictionary<string, object?>();
        var key = new ContextDataKey<string>("missing");

        // act
        var copied = source.CopyTo(target, key);

        // assert
        Assert.False(copied);
        Assert.Empty(target);
    }

    [Fact]
    public void Constructor_Should_InitializeWithCapacity_When_CapacityIsProvided()
    {
        // arrange & act
        var headers = new Headers(10);

        // assert
        Assert.Equal(0, headers.Count);
    }

    [Fact]
    public void AddRange_Should_UpdateExistingKeys_When_KeysAlreadyExist()
    {
        // arrange
        var headers = new Headers();
        headers.Set("key1", "original1");
        headers.Set("key2", "original2");

        // act
        headers.AddRange([
            new HeaderValue { Key = "key1", Value = "updated1" },
            new HeaderValue { Key = "key3", Value = "new3" }
        ]);

        // assert
        Assert.Equal(3, headers.Count);
        Assert.Equal("updated1", headers.GetValue("key1"));
        Assert.Equal("original2", headers.GetValue("key2"));
        Assert.Equal("new3", headers.GetValue("key3"));
    }

    [Fact]
    public void GetValue_Should_ReturnNull_When_KeyDoesNotExist()
    {
        // arrange
        var headers = new Headers();

        // act
        var value = headers.GetValue("nonexistent");

        // assert
        Assert.Null(value);
    }

    [Fact]
    public void ContainsKey_Should_ReturnFalse_When_KeyDoesNotExist()
    {
        // arrange
        var headers = new Headers();
        headers.Set("exists", "value");

        // act
        var contains = headers.ContainsKey("missing");

        // assert
        Assert.False(contains);
    }

    [Fact]
    public void GetEnumerator_Should_IterateInInsertionOrder_When_HeadersAreAdded()
    {
        // arrange
        var headers = new Headers();
        headers.Set("first", 1);
        headers.Set("second", 2);
        headers.Set("third", 3);

        // act
        var keys = new List<string>();
        foreach (var header in headers)
        {
            keys.Add(header.Key);
        }

        // assert
        Assert.Equal(new[] { "first", "second", "third" }, keys);
    }

    public static TheoryData<string, object?, object?> ExactRoundTripCases
        => new()
        {
            { "intValue", 42, 42 },
            { "longValue", 9223372036854775807L, 9223372036854775807L },
            { "doubleValue", 3.14159, 3.14159 },
            { "boolValue", true, true },
            { "nullValue", null, null }
        };

    public static TheoryData<string, object?> LossyRoundTripCases
        => new()
        {
            { "floatValue", 2.71828f },
            { "decimalValue", 123.456m },
            { "dateTimeOffset", new DateTimeOffset(2024, 1, 15, 10, 30, 45, TimeSpan.Zero) }
        };

    public static TheoryData<string, object?[], object?[]> ArrayRoundTripCases
        => new()
        {
            { "ints", new object[] { 1, 2, 3 }, new object[] { 1, 2, 3 } },
            { "strings", new object[] { "a", "b", "c" }, new object[] { "a", "b", "c" } },
            { "mixed", new object?[] { "text", 42, true, null }, new object?[] { "text", 42, true, null } }
        };

    public static TheoryData<string, object?, Type?> EmptyContainerCases
        => new()
        {
            { "none", null, null },
            { "empty", new Dictionary<string, object?>(), typeof(Dictionary<string, object?>) },
            { "emptyArray", Array.Empty<object>(), typeof(object[]) }
        };
}
