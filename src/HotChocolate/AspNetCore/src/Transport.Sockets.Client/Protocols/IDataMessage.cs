namespace HotChocolate.Transport.Sockets.Client;

interface IDataMessage : IOperationMessage
{
    string Id { get; }
}
