using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// <see cref="MochaResource"/> describing a registered consumer (handler) on the bus.
/// </summary>
internal sealed class MochaHandlerResource : MochaResource
{
    private readonly string _id;
    private readonly string _name;
    private readonly string _identityType;
    private readonly string? _identityTypeFullName;
    private readonly string? _sagaName;
    private readonly bool _isBatch;

    public MochaHandlerResource(string instanceId, ConsumerDescription description)
    {
        _name = description.Name;
        _identityType = description.IdentityType;
        _identityTypeFullName = description.IdentityTypeFullName;
        _sagaName = description.SagaName;
        _isBatch = description.IsBatch;
        _id = MochaUrn.Create("core", "handler", instanceId, description.Name);
    }

    public override string Kind => "mocha.handler";

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("name", _name);
        writer.WriteString("identity_type", _identityType);

        if (_identityTypeFullName is not null)
        {
            writer.WriteString("identity_type_full_name", _identityTypeFullName);
        }

        if (_sagaName is not null)
        {
            writer.WriteString("saga_name", _sagaName);
        }

        writer.WriteBoolean("is_batch", _isBatch);
    }
}
