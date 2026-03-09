using System.Threading.Channels;
using HotChocolate.Subscriptions.Properties;

namespace HotChocolate.Subscriptions;

internal static class TopicBufferFullModeExtensions
{
    public static BoundedChannelFullMode ToBoundedChannelFullMode(this TopicBufferFullMode fullMode)
        => fullMode switch
        {
            TopicBufferFullMode.DropNewest => BoundedChannelFullMode.DropNewest,
            TopicBufferFullMode.DropOldest => BoundedChannelFullMode.DropOldest,
            TopicBufferFullMode.DropWrite => BoundedChannelFullMode.DropWrite,
            _ => throw new ArgumentOutOfRangeException(
                nameof(fullMode),
                fullMode,
                Resources.ConvertFullMode_Value_NotSupported)
        };
}
