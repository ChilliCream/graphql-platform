using System.Text.Json.Serialization;
using AotExample.Contracts.Events;
using AotExample.Contracts.Requests;

namespace AotExample.Contracts;

[JsonSerializable(typeof(OrderPlacedEvent))]
[JsonSerializable(typeof(OrderShippedEvent))]
[JsonSerializable(typeof(CheckInventoryRequest))]
[JsonSerializable(typeof(CheckInventoryResponse))]
public partial class AotExampleJsonContext : JsonSerializerContext;
