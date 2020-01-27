namespace MarshmallowPie.Processing
{
    public interface ISessionMessage
    {
        string SessionId { get; }

        bool IsCompleted { get; }
    }
}
