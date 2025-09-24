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
}
