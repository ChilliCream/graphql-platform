using System;
using System.Text.Json;

namespace HotChocolate.Transport.Sockets.Client;

internal interface IOperationMessage
{
    string Type { get; }
}

interface IDataMessage : IOperationMessage
{
    string Id { get; }
}
