namespace HotChocolate.Transport.Sockets.Client.Protocols;

internal interface IDataCompletion
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
