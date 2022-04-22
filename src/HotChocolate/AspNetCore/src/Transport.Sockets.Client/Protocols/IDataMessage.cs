namespace HotChocolate.Transport.Sockets.Client.Protocols;

interface IDataMessage : IOperationMessage
{
    string Id { get; }
}
