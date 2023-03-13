namespace HotChocolate.Transport.Sockets.Client.Protocols;

/// <summary>
/// Represents an abstraction for marking a data stream as completed and sending a data complete
/// message to the server.
/// </summary>
internal interface IDataCompletion
{
    /// <summary>
    /// Marks the data stream as completed.
    /// </summary>
    void MarkDataStreamCompleted();

    /// <summary>
    /// Tries to send a data complete message to the server.
    /// </summary>
    void TrySendCompleteMessage();
}
