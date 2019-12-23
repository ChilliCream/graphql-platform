using System.Net.Http;

namespace StrawberryShake.Transport
{
    public static class MessageWriterExtensions
    {
        public static ByteArrayContent ToByteArrayContent(this IRequestWriter writer)
        {
            return new ByteArrayContent(writer.GetInternalBuffer(), 0, writer.Length);
        }
    }
}
