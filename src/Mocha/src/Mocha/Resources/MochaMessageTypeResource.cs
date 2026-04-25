using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// <see cref="MochaResource"/> describing a registered message type on the bus.
/// </summary>
internal sealed class MochaMessageTypeResource : MochaResource
{
    private readonly string _id;
    private readonly string _identity;
    private readonly string _runtimeType;
    private readonly string? _runtimeTypeFullName;
    private readonly bool _isInterface;
    private readonly bool _isInternal;
    private readonly string? _defaultContentType;
    private readonly IReadOnlyList<string>? _enclosedMessageIdentities;

    public MochaMessageTypeResource(string instanceId, MessageTypeDescription description)
    {
        _identity = description.Identity;
        _runtimeType = description.RuntimeType;
        _runtimeTypeFullName = description.RuntimeTypeFullName;
        _isInterface = description.IsInterface;
        _isInternal = description.IsInternal;
        _defaultContentType = description.DefaultContentType;
        _enclosedMessageIdentities = description.EnclosedMessageIdentities;
        _id = MochaUrn.Create("core", "message_type", instanceId, description.Identity);
    }

    public override string Kind => "mocha.message_type";

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("identity", _identity);
        writer.WriteString("runtime_type", _runtimeType);

        if (_runtimeTypeFullName is not null)
        {
            writer.WriteString("runtime_type_full_name", _runtimeTypeFullName);
        }

        writer.WriteBoolean("is_interface", _isInterface);
        writer.WriteBoolean("is_internal", _isInternal);

        if (_defaultContentType is not null)
        {
            writer.WriteString("default_content_type", _defaultContentType);
        }

        if (_enclosedMessageIdentities is { Count: > 0 })
        {
            writer.WriteStartArray("enclosed_message_identities");
            foreach (var enclosed in _enclosedMessageIdentities)
            {
                writer.WriteStringValue(enclosed);
            }

            writer.WriteEndArray();
        }
    }
}
