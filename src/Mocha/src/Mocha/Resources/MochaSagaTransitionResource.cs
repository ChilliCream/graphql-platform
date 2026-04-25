using System.Globalization;
using System.Text.Json;
using Mocha.Resources;
using Mocha.Sagas;

namespace Mocha;

/// <summary>
/// <see cref="MochaResource"/> describing a transition between two saga states triggered by a
/// specific event.
/// </summary>
internal sealed class MochaSagaTransitionResource : MochaResource
{
    private readonly string _id;
    private readonly string _sagaId;
    private readonly string _fromStateId;
    private readonly string _toStateId;
    private readonly string _eventType;
    private readonly string? _eventTypeFullName;
    private readonly SagaTransitionKind _transitionKind;
    private readonly bool _autoProvision;

    public MochaSagaTransitionResource(
        string sagaId,
        string fromStateId,
        string toStateId,
        string instanceId,
        string sagaName,
        string fromStateName,
        int index,
        SagaTransitionDescription description)
    {
        _sagaId = sagaId;
        _fromStateId = fromStateId;
        _toStateId = toStateId;
        _eventType = description.EventType;
        _eventTypeFullName = description.EventTypeFullName;
        _transitionKind = description.TransitionKind;
        _autoProvision = description.AutoProvision;
        _id = MochaUrn.Create(
            "core",
            "saga_transition",
            instanceId,
            sagaName,
            fromStateName,
            description.EventType,
            index.ToString(CultureInfo.InvariantCulture));
    }

    public override string Kind => "mocha.saga.transition";

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("saga_id", _sagaId);
        writer.WriteString("from_state_id", _fromStateId);
        writer.WriteString("to_state_id", _toStateId);
        writer.WriteString("event_type", _eventType);

        if (_eventTypeFullName is not null)
        {
            writer.WriteString("event_type_full_name", _eventTypeFullName);
        }

        writer.WriteString("transition_kind", _transitionKind.ToString());
        writer.WriteBoolean("auto_provision", _autoProvision);
    }
}
