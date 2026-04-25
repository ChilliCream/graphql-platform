using System.Text.Json;
using Mocha.Resources;

namespace Mocha;

/// <summary>
/// <see cref="MochaResource"/> describing a single state within a saga state machine.
/// </summary>
internal sealed class MochaSagaStateResource : MochaResource
{
    private readonly string _id;
    private readonly string _sagaId;
    private readonly string _name;
    private readonly bool _isInitial;
    private readonly bool _isFinal;

    public MochaSagaStateResource(string sagaId, string instanceId, string sagaName, SagaStateDescription description)
    {
        _sagaId = sagaId;
        _name = description.Name;
        _isInitial = description.IsInitial;
        _isFinal = description.IsFinal;
        _id = MochaUrn.Create("core", "saga_state", instanceId, sagaName, description.Name);
    }

    public override string Kind => "mocha.saga.state";

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("saga_id", _sagaId);
        writer.WriteString("name", _name);
        writer.WriteBoolean("is_initial", _isInitial);
        writer.WriteBoolean("is_final", _isFinal);
    }
}
