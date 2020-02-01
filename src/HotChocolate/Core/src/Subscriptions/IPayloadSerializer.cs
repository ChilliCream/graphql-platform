namespace HotChocolate.Subscriptions
{
    public interface IPayloadSerializer
    {
        byte[] Serialize(object value);

        object Deserialize(byte[] content);
    }
}
