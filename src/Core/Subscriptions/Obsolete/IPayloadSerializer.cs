using System;

namespace HotChocolate.Subscriptions
{
    [Obsolete("This type will be removed and has no replacement.")]
    public interface IPayloadSerializer
    {
        byte[] Serialize(object value);

        object Deserialize(byte[] content);
    }
}
