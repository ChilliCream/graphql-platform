namespace MarshmallowPie.Processing
{
    public class PublishSchemaEvent
        : ISessionMessage
    {
        public PublishSchemaEvent(string sessionId, Issue issue)
        {
            SessionId = sessionId;
            Issue = issue;
            IsCompleted = false;
        }

        private PublishSchemaEvent(string sessionId)
        {
            SessionId = sessionId;
            Issue = null;
            IsCompleted = true;
        }

        public string SessionId { get; }

        public Issue? Issue { get; }

        public bool IsCompleted { get; }

        public static PublishSchemaEvent Completed(string sessionId) =>
            new PublishSchemaEvent(sessionId);
    }
}
