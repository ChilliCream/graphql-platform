namespace HotChocolate.Transport.Sockets.Client.Protocols;

internal interface IDataCompleteTask
{
    /// <summary>
    /// Marks the data stream as completed.
    /// </summary>
    void SetCompleted();

    /// <summary>
    /// Tries to send a data complete message to the server.
    /// </summary>
    void TryComplete();
}
