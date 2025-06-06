using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace HotChocolate.Subscriptions;

internal sealed class TopicFormatter
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;
    private readonly ThreadLocal<MD5> _md5 = new(MD5.Create);
    private readonly byte[] _prefix;

    public TopicFormatter(string? prefix)
    {
        _prefix = prefix is not null ? s_utf8.GetBytes(prefix) : [];
    }

    public string Format(string topic)
    {
        var length = checked(1 + (topic.Length * 4) + _prefix.Length);
        byte[]? topicBuffer = null;

        var topicSpan = length <= 256
            ? stackalloc byte[length]
            : topicBuffer = ArrayPool<byte>.Shared.Rent(length);

        if (_prefix.Length > 0)
        {
            _prefix.AsSpan().CopyTo(topicSpan);
            topicSpan[_prefix.Length] = (byte)':';
            var topicLength = s_utf8.GetBytes(topic, topicSpan[(_prefix.Length + 1)..]);
            topicSpan = topicSpan[..(_prefix.Length + 1 + topicLength)];
        }
        else
        {
            var topicLength = s_utf8.GetBytes(topic, topicSpan);
            topicSpan = topicSpan[..topicLength];
        }

        var hashBytes = new byte[16];
        var md5 = _md5.Value ??= MD5.Create();
        md5.TryComputeHash(topicSpan, hashBytes, out var bytesWritten);

        var hashSpan = hashBytes.AsSpan();
        if (bytesWritten < 16)
        {
            hashSpan = hashSpan[..bytesWritten];
        }

        var topicString = Convert.ToHexString(hashSpan);

        if (topicBuffer is not null)
        {
            topicSpan.Clear();
            ArrayPool<byte>.Shared.Return(topicBuffer);
        }

        return topicString;
    }
}
