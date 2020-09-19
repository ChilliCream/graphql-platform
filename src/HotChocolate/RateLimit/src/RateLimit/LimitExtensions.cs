#if NETSTANDARD2_0
using System.Text;
using Newtonsoft.Json;
#else
using System;
#endif

namespace HotChocolate.RateLimit
{
    internal static class LimitExtensions
    {
        internal static byte[] ToByte(this Limit limit)
        {
#if NETSTANDARD2_0
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(limit));
#else
            Span<byte> payload = stackalloc byte[12];

            BitConverter.TryWriteBytes(payload.Slice(0, 8),limit.Timestamp.Ticks);
            BitConverter.TryWriteBytes(payload.Slice(8, 4), limit.Requests);

            return payload.ToArray();
#endif
        }

        internal static Limit ToLimit(this byte[] payload)
        {
#if NETSTANDARD2_0
            return JsonConvert.DeserializeObject<Limit>(Encoding.UTF8.GetString(payload));
#else
            Span<byte> span = payload.AsSpan();
            var timestamp = DateTime.FromBinary(BitConverter.ToInt64(span.Slice(0, 8)));
            var requests = BitConverter.ToInt32(span.Slice(8, 4));

            return Limit.Create(timestamp, requests);
#endif
        }
    }
}
