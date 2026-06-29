using System.Text.Json.Serialization;
using AotExample.OrderService.Sagas;

namespace AotExample.OrderService;

[JsonSerializable(typeof(OrderSagaState))]
public partial class OrderServiceJsonContext : JsonSerializerContext;
