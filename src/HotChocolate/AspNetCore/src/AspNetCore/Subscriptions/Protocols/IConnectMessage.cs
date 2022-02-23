using System.Text.Json;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

public interface IConnectMessage
{
    JsonElement? Payload { get; }
}
