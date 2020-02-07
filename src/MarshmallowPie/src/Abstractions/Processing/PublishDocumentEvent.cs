namespace MarshmallowPie.Processing
{
    public class PublishDocumentEvent
        : ISessionMessage
    {
        public PublishDocumentEvent(string sessionId, Issue issue)
        {
            SessionId = sessionId;
            Issue = issue;
            IsCompleted = false;
        }

        private PublishDocumentEvent(string sessionId)
        {
            SessionId = sessionId;
            Issue = null;
            IsCompleted = true;
        }

        public string SessionId { get; }

        public Issue? Issue { get; }

        public bool IsCompleted { get; }

        public static PublishDocumentEvent Completed(string sessionId) =>
            new PublishDocumentEvent(sessionId);
    }
}
