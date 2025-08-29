#if NET8_0 || NETSTANDARD2_0
using System.Text;
using System.Text.Json;

namespace System.Runtime.InteropServices;

public static class JsonMarshal
{
    public static ReadOnlySpan<byte> GetRawUtf8Value(JsonElement value)
    {
        return Encoding.UTF8.GetBytes(value.GetRawText());
    }
}
#endif
