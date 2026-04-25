using System.Text.Json;
using Mocha.Resources;

namespace Mocha;

/// <summary>
/// <see cref="MochaResource"/> describing a registered saga state machine.
/// </summary>
internal sealed class MochaSagaResource : MochaResource
{
    private readonly string _id;
    private readonly string _name;
    private readonly string _stateType;
    private readonly string? _stateTypeFullName;
    private readonly string _consumerName;

    public MochaSagaResource(string instanceId, SagaDescription description)
    {
        _name = description.Name;
        _stateType = description.StateType;
        _stateTypeFullName = description.StateTypeFullName;
        _consumerName = description.ConsumerName;
        _id = MochaUrn.Create("core", "saga", instanceId, description.Name);
    }

    public override string Kind => "mocha.saga";

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("name", _name);
        writer.WriteString("state_type", _stateType);

        if (_stateTypeFullName is not null)
        {
            writer.WriteString("state_type_full_name", _stateTypeFullName);
        }

        writer.WriteString("consumer_name", _consumerName);
    }
}
