using System.Text.Json.Serialization;

namespace Mocha.Events;

[JsonSerializable(typeof(AcknowledgedEvent))]
[JsonSerializable(typeof(NotAcknowledgedEvent))]
internal partial class AcknowledgementJsonContext : JsonSerializerContext;
