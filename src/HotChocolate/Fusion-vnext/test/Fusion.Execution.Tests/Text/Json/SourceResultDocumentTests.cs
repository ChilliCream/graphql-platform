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
    public void EnumerateProperty()
    {
        var json = """
                   {
                     "a": 1,
                     "b": "abc",
                     "c": [1, 2, 3]
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

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void EnumerateArray()
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
}
