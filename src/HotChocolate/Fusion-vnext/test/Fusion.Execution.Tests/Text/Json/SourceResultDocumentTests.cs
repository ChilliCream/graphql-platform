using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public class SourceResultDocumentTests
{
    [Fact]
    public void TryGetProperty_String_Name()
    {
        var json = """
                   {
                     "user": {
                       "name": "John",
                       "age": 30
                     },
                     "items": [1, 2, 3]
                   }
                   """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        if (result.Root.TryGetProperty("user", out var user))
        {
            Assert.Equal(JsonValueKind.Object, user.ValueKind);

            if (user.TryGetProperty("name", out var name))
            {
                Assert.Equal("John", name.AssertString());
                return;
            }
        }

        Assert.Fail("We should not get here.");
    }

    [Fact]
    public void GetProperty_String_Name()
    {
        var json = """
                   {
                     "user": {
                       "name": "John",
                       "age": 30
                     },
                     "items": [1, 2, 3]
                   }
                   """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        var user = result.Root.GetProperty("user");
        Assert.Equal(JsonValueKind.Object, user.ValueKind);
    }

    [Fact]
    public void TryGetProperty_Span_Name()
    {
        var json = """
                   {
                     "user": {
                       "name": "John",
                       "age": 30
                     },
                     "items": [1, 2, 3]
                   }
                   """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        if (result.Root.TryGetProperty("user"u8, out var user))
        {
            Assert.Equal(JsonValueKind.Object, user.ValueKind);

            if (user.TryGetProperty("name"u8, out var name))
            {
                Assert.Equal("John", name.AssertString());
                return;
            }
        }

        Assert.Fail("We should not get here.");
    }

    [Fact]
    public void GetProperty_Span_Name()
    {
        var json = """
                   {
                     "user": {
                       "name": "John",
                       "age": 30
                     },
                     "items": [1, 2, 3]
                   }
                   """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        var user = result.Root.GetProperty("user"u8);
        Assert.Equal(JsonValueKind.Object, user.ValueKind);
    }

    [Fact]
    public void EnumerateProperty_Mixed_Values()
    {
        var json = """
                   {
                     "a": 1,
                     "b": "abc",
                     "c": [1, 2, 3],
                     "d": { "a": 1 }
                   }
                   """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);

        using var enumerator = result.Root.EnumerateObject().GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal("a", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.Number, enumerator.Current.Value.ValueKind);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("b", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.String, enumerator.Current.Value.ValueKind);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("c", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.Array, enumerator.Current.Value.ValueKind);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("d", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.Object, enumerator.Current.Value.ValueKind);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void EnumerateProperty_Leafs()
    {
        var json = """
                   {
                     "a": 1,
                     "b": 2,
                     "c": 3,
                     "d": 4
                   }
                   """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);

        using var enumerator = result.Root.EnumerateObject().GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal("a", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.Number, enumerator.Current.Value.ValueKind);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("b", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.Number, enumerator.Current.Value.ValueKind);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("c", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.Number, enumerator.Current.Value.ValueKind);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("d", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.Number, enumerator.Current.Value.ValueKind);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void EnumerateProperty_Objects()
    {
        var json = """
                   {
                     "a": { "a": 1 },
                     "b": { "a": 1 },
                     "c": { "a": 1 },
                     "d": { "a": 1 }
                   }
                   """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);

        using var enumerator = result.Root.EnumerateObject().GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal("a", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.Object, enumerator.Current.Value.ValueKind);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("b", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.Object, enumerator.Current.Value.ValueKind);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("c", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.Object, enumerator.Current.Value.ValueKind);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("d", enumerator.Current.Name);
        Assert.Equal(JsonValueKind.Object, enumerator.Current.Value.ValueKind);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void EnumerateArray_Leafs()
    {
        var json = """
                   {
                     "a": [1, 2, 3]
                   }
                   """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        var prop = result.Root.GetProperty("a");
        using var enumerator = prop.EnumerateArray().GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current.GetInt32());

        Assert.True(enumerator.MoveNext());
        Assert.Equal(2, enumerator.Current.GetInt32());

        Assert.True(enumerator.MoveNext());
        Assert.Equal(3, enumerator.Current.GetInt32());

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void EnumerateArray_Objects()
    {
        var json = """
                   {
                     "a": [{ "a" : 1 }, { "a" : 1 }, { "a" : 1 }]
                   }
                   """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        var prop = result.Root.GetProperty("a");
        using var enumerator = prop.EnumerateArray().GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal(JsonValueKind.Object, enumerator.Current.ValueKind);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(JsonValueKind.Object, enumerator.Current.ValueKind);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(JsonValueKind.Object, enumerator.Current.ValueKind);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Parse_CrossChunk_LongString_And_Array()
    {
        // Build a JSON payload that exceeds one 128 KiB chunk so it must span two chunks.
        const int chunkSize = 128 * 1024; // 131072
        const int blobChars = 130 * 1024; // 133120 > 1 chunk

        var sb = new StringBuilder();
        sb.Append("{\"a\":[1,2,3],\"blob\":\"");
        sb.Append('x', blobChars);
        sb.Append("\"}");

        var json = Encoding.UTF8.GetBytes(sb.ToString());
        Assert.True(json.Length > chunkSize);
        Assert.True(json.Length < 2 * chunkSize); // keep it inside two chunks for the test

        var chunk1 = new byte[chunkSize];
        var chunk2 = new byte[chunkSize];

        json.AsSpan(0, chunkSize).CopyTo(chunk1);
        json.AsSpan(chunkSize).CopyTo(chunk2);

        var result = SourceResultDocument.Parse(
            [chunk1, chunk2],
            json.Length - chunkSize,
            2,
            pooledMemory: false);

        // Assert small array parses and enumerates correctly.
        var a = result.Root.GetProperty("a");
        using var e = a.EnumerateArray().GetEnumerator();
        Assert.True(e.MoveNext());
        Assert.Equal(1, e.Current.GetInt32());
        Assert.True(e.MoveNext());
        Assert.Equal(2, e.Current.GetInt32());
        Assert.True(e.MoveNext());
        Assert.Equal(3, e.Current.GetInt32());
        Assert.False(e.MoveNext());

        // Assert the large string crosses the boundary intact.
        var blob = result.Root.GetProperty("blob").AssertString();
        Assert.Equal(blobChars, blob.Length);
        Assert.Equal('x', blob[0]);
        Assert.Equal('x', blob[^1]);
    }

    [Fact]
    public void Parse_CrossChunk_LongEscapedString()
    {
        // Build a long string (> 1 chunk) that contains escape sequences
        const int chunkSize = 128 * 1024; // 131072
        const int baseLen = 130 * 1024; // 133120 > 1 chunk

        var sb = new StringBuilder();
        sb.Append("{\"blob\":\"");

        // Insert escapes approximately every ~1024 chars
        var remaining = baseLen;
        var block = new string('x', 1016);
        while (remaining > 0)
        {
            var take = Math.Min(1016, remaining);
            sb.Append(block.AsSpan(0, take));
            remaining -= take;
            if (remaining > 0)
            {
                // add a mix of escapes so unescape path is exercised
                sb.Append("\\n");       // newline
                sb.Append("\\t");       // tab
                sb.Append("\\u0041");  // 'A'
            }
        }

        sb.Append("\"}");

        var json = Encoding.UTF8.GetBytes(sb.ToString());
        Assert.True(json.Length > chunkSize); // ensure cross-chunk

        var chunk1 = new byte[chunkSize];
        var chunk2 = new byte[chunkSize];
        json.AsSpan(0, chunkSize).CopyTo(chunk1);
        json.AsSpan(chunkSize).CopyTo(chunk2);

        // last arg is bytes used in the last chunk
        var result = SourceResultDocument.Parse(
            [chunk1, chunk2],
            json.Length - chunkSize,
            2,
            pooledMemory: false);

        // Compare against System.Text.Json to validate unescape correctness
        using var stj = JsonDocument.Parse(json);
        var expected = stj.RootElement.GetProperty("blob").GetString();
        var actual = result.Root.GetProperty("blob").AssertString();

        Assert.Equal(expected, actual);
        Assert.Contains('\n', actual);
        Assert.Contains('\t', actual);
        Assert.Contains('A', actual); // from \u0041
    }

    [Fact]
    public void EnumerateArray_Objects_CrossChunk_Many()
    {
        const int chunkSize = 128 * 1024;
        const int elements = 500_000;

        var sb = new StringBuilder();
        sb.Append("{\"a\":[");
        for (var i = 0; i < elements; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append("{\"v\":1}");
        }
        sb.Append("]}");

        var json = Encoding.UTF8.GetBytes(sb.ToString());

        // Calculate number of chunks needed
        var chunkCount = (json.Length + chunkSize - 1) / chunkSize;
        var chunks = new byte[chunkCount][];

        // Generate chunks dynamically, each exactly 128KB
        for (var i = 0; i < chunkCount; i++)
        {
            chunks[i] = new byte[chunkSize];
            var sourceOffset = i * chunkSize;
            var bytesToCopy = Math.Min(chunkSize, json.Length - sourceOffset);

            if (bytesToCopy > 0)
            {
                json.AsSpan(sourceOffset, bytesToCopy).CopyTo(chunks[i]);
            }
            // Remaining bytes in chunk are already zero-initialized
        }

        // Calculate actual data length in the last chunk
        var lastChunkDataLength = json.Length % chunkSize;
        if (lastChunkDataLength == 0 && chunkCount > 0)
        {
            lastChunkDataLength = chunkSize;
        }

        var result = SourceResultDocument.Parse(
            chunks,
            lastChunkDataLength,
            chunkCount,
            pooledMemory: false);
        var prop = result.Root.GetProperty("a");

        var count = 0;
        foreach (var el in prop.EnumerateArray())
        {
            Assert.Equal(JsonValueKind.Object, el.ValueKind);
            Assert.Equal(1, el.GetProperty("v").GetInt32());
            count++;
        }

        Assert.Equal(elements, count);
    }

    [Fact]
    public void Parse_EmptyObject_Success()
    {
        var json = "{}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        Assert.Equal(JsonValueKind.Object, result.Root.ValueKind);
        Assert.Equal(0, result.Root.GetPropertyCount());
    }

    [Fact]
    public void Parse_EmptyArray_Success()
    {
        var json = "{\"arr\": []}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        var arr = result.Root.GetProperty("arr");
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public void GetProperty_NonExistent_ThrowsKeyNotFoundException()
    {
        var json = "{\"a\": 1}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        Assert.Throws<KeyNotFoundException>(() => result.Root.GetProperty("nonexistent"));
    }

    [Fact]
    public void ArrayAccess_OutOfBounds_ThrowsIndexOutOfRange()
    {
        var json = "{\"arr\": [1, 2, 3]}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        var arr = result.Root.GetProperty("arr");
        Assert.Throws<IndexOutOfRangeException>(() => arr[5]);
    }

    [Fact]
    public void Parse_AllNumericTypes_Success()
    {
        var json = """
        {
            "sbyte": -128,
            "byte": 255,
            "short": -32768,
            "ushort": 65535,
            "int": -2147483648,
            "uint": 4294967295,
            "long": -9223372036854775808,
            "ulong": 18446744073709551615,
            "float": 3.14,
            "double": 3.141592653589793,
            "decimal": 79228162514264337593543950335,
            "scientific": 1.23e10
        }
        """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);

        Assert.True(result.Root.GetProperty("sbyte").TryGetSByte(out var sb));
        Assert.Equal(-128, sb);

        Assert.True(result.Root.GetProperty("byte").TryGetByte(out var b));
        Assert.Equal(255, b);

        Assert.True(result.Root.GetProperty("scientific").TryGetDouble(out var sci));
        Assert.Equal(1.23e10, sci);
    }

    [Fact]
    public void Parse_InvalidNumericConversion_ReturnsFalse()
    {
        var json = "{\"big\": 999999999999999999999999}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        Assert.False(result.Root.GetProperty("big").TryGetInt32(out _));
    }

    [Fact]
    public void Parse_EscapedStrings_UnescapesCorrectly()
    {
        var json = """
        {
            "newline": "line1\nline2",
            "tab": "col1\tcol2",
            "quote": "say \"hello\"",
            "backslash": "path\\to\\file",
            "unicode": "\u0041\u0042\u0043"
        }
        """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);

        Assert.Equal("line1\nline2", result.Root.GetProperty("newline").GetString());
        Assert.Equal("say \"hello\"", result.Root.GetProperty("quote").GetString());
        Assert.Equal("ABC", result.Root.GetProperty("unicode").GetString());
    }

    [Fact]
    public void ValueEquals_WithEscapedStrings_WorksCorrectly()
    {
        var json = "{\"escaped\": \"hello\\nworld\"}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        var prop = result.Root.GetProperty("escaped");

        Assert.True(prop.ValueEquals("hello\nworld"));
        Assert.False(prop.ValueEquals("hello\\nworld"));
    }

    [Fact]
    public void TryGetProperty_EscapedPropertyNames_WorksCorrectly()
    {
        var json = "{\"prop\\nname\": 42}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);

        Assert.True(result.Root.TryGetProperty("prop\nname", out var value));
        Assert.Equal(42, value.GetInt32());
    }

    [Fact]
    public void TryGetProperty_DuplicatePropertyNames_ReturnsLast()
    {
        var json = "{\"key\": 1, \"key\": 2, \"key\": 3}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        Assert.Equal(3, result.Root.GetProperty("key").GetInt32());
    }

    [Fact]
    public void Parse_DeeplyNestedStructure_Success()
    {
        var json = """
        {
            "level1": {
                "level2": {
                    "level3": {
                        "level4": {
                            "value": "deep"
                        }
                    }
                }
            }
        }
        """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        var deep = result.Root
            .GetProperty("level1")
            .GetProperty("level2")
            .GetProperty("level3")
            .GetProperty("level4")
            .GetProperty("value");

        Assert.Equal("deep", deep.GetString());
    }

    [Fact]
    public void Parse_NestedArraysAndObjects_Success()
    {
        var json = """
        {
            "matrix": [
                [1, 2, 3],
                [4, 5, 6]
            ],
            "users": [
                {"name": "Alice", "scores": [95, 87]},
                {"name": "Bob", "scores": [82, 91]}
            ]
        }
        """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        var firstUser = result.Root.GetProperty("users")[0];
        Assert.Equal("Alice", firstUser.GetProperty("name").GetString());
        Assert.Equal(95, firstUser.GetProperty("scores")[0].GetInt32());
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        var json = "{\"a\": 1}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        result.Dispose();
        result.Dispose(); // Should not throw
    }

    [Fact]
    public void Access_AfterDispose_ThrowsObjectDisposedException()
    {
        var json = "{\"a\": 1}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        result.Dispose();

        Assert.Throws<ObjectDisposedException>(() => result.Root.GetProperty("a"));
    }

    [Fact]
    public void Parse_BooleanAndNullValues_Success()
    {
        var json = """
        {
            "isTrue": true,
            "isFalse": false,
            "isNull": null
        }
        """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);

        Assert.True(result.Root.GetProperty("isTrue").GetBoolean());
        Assert.False(result.Root.GetProperty("isFalse").GetBoolean());
        Assert.Null(result.Root.GetProperty("isNull").GetString());
    }

    [Fact]
    public void ValueEquals_WithStrings_WorksCorrectly()
    {
        var json = "{\"str\": \"test value\"}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        var prop = result.Root.GetProperty("str");

        Assert.True(prop.ValueEquals("test value"));
        Assert.False(prop.ValueEquals("different value"));
        Assert.True(prop.ValueEquals("test value"u8));
        Assert.False(prop.ValueEquals("different value"u8));
    }

    [Fact]
    public void ValueEquals_WithNull_WorksCorrectly()
    {
        var json = "{\"null\": null, \"str\": \"notNull\"}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);

        Assert.True(result.Root.GetProperty("null").ValueEquals((string?)null));
        Assert.False(result.Root.GetProperty("str").ValueEquals((string?)null));
    }

    [Fact]
    public void ArrayAccess_ByIndex_WorksCorrectly()
    {
        var json = "{\"arr\": [\"zero\", \"one\", \"two\"]}"u8.ToArray();
        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);
        var arr = result.Root.GetProperty("arr");

        Assert.Equal("zero", arr[0].GetString());
        Assert.Equal("one", arr[1].GetString());
        Assert.Equal("two", arr[2].GetString());
        Assert.Equal(3, arr.GetArrayLength());
    }

    [Fact]
    public void EnumerateObject_PropertyNameEquals_WorksCorrectly()
    {
        var json = """
        {
            "name": "Alice",
            "age": 30,
            "city": "NYC"
        }
        """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);

        var foundNames = new List<string>();
        foreach (var property in result.Root.EnumerateObject())
        {
            foundNames.Add(property.Name);

            if (property.NameEquals("name"))
            {
                Assert.Equal("Alice", property.Value.GetString());
            }
            else if (property.NameEquals("age"))
            {
                Assert.Equal(30, property.Value.GetInt32());
            }
        }

        Assert.Equal(["name", "age", "city"], foundNames);
    }

    [Fact]
    public void GetRawText_ReturnsOriginalJson_Success()
    {
        var json = """
        {
            "number": 42,
            "string": "hello",
            "object": {"nested": true}
        }
        """u8.ToArray();

        var chunk = new byte[128 * 1024];
        json.AsSpan().CopyTo(chunk);

        var result = SourceResultDocument.Parse([chunk], json.Length, 1, pooledMemory: false);

        Assert.Equal("42", result.Root.GetProperty("number").GetRawText());
        Assert.Equal("\"hello\"", result.Root.GetProperty("string").GetRawText());
        Assert.Contains("nested", result.Root.GetProperty("object").GetRawText());
    }
}
