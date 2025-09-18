using System.Text;

namespace HotChocolate.Fusion.Text.Json;

public class SourceResultDocumentTests
{
    [Fact]
    public void Test()
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

        SourceResultDocument.Parse([chunk], json.Length);
    }
}
