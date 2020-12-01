namespace StrawberryShake.Transport.WebSockets.Messages
{
    public class OperationMessage<T>
        : OperationMessage
    {
        public OperationMessage(string type, T payload)
            : base(type)
        {
            Payload = payload;
        }

        public OperationMessage(string type, string id, T payload)
            : base(type, id)
        {
            Payload = payload;
        }

        public T Payload { get; }
    }
}
