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

        var result = SourceResultDocument.Parse([chunk], json.Length);
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

        var result = SourceResultDocument.Parse([chunk], json.Length);
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

        var result = SourceResultDocument.Parse([chunk], json.Length);
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

        var result = SourceResultDocument.Parse([chunk], json.Length);
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

        var result = SourceResultDocument.Parse([chunk], json.Length);

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

        var result = SourceResultDocument.Parse([chunk], json.Length);

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

        var result = SourceResultDocument.Parse([chunk], json.Length);

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

        var result = SourceResultDocument.Parse([chunk], json.Length);
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

        var result = SourceResultDocument.Parse([chunk], json.Length);
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

        var result = SourceResultDocument.Parse([chunk1, chunk2], json.Length - chunkSize);

        // Assert small array parses and enumerates correctly.
        var a = result.Root.GetProperty("a");
        using var e = a.EnumerateArray().GetEnumerator();
        Assert.True(e.MoveNext()); Assert.Equal(1, e.Current.GetInt32());
        Assert.True(e.MoveNext()); Assert.Equal(2, e.Current.GetInt32());
        Assert.True(e.MoveNext()); Assert.Equal(3, e.Current.GetInt32());
        Assert.False(e.MoveNext());

        // Assert the large string crosses the boundary intact.
        var blob = result.Root.GetProperty("blob").AssertString();
        Assert.Equal(blobChars, blob.Length);
        Assert.Equal('x', blob[0]);
        Assert.Equal('x', blob[^1]);
    }
}
