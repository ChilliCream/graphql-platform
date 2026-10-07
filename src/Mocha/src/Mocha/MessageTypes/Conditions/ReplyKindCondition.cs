using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Matches reply messages, and fault notifications when faults are included.
/// </summary>
internal sealed class ReplyKindCondition : RouteCondition
{
    private readonly bool _includeFaults;

    private ReplyKindCondition(bool includeFaults)
    {
        _includeFaults = includeFaults;
    }

    /// <summary>
    /// Gets the condition that matches successful replies only.
    /// </summary>
    public static ReplyKindCondition Reply { get; } = new(includeFaults: false);

    /// <summary>
    /// Gets the condition that matches successful replies and fault notifications.
    /// </summary>
    public static ReplyKindCondition ReplyOrFault { get; } = new(includeFaults: true);

    /// <inheritdoc />
    public override bool Matches(IReceiveContext context)
        => context.Headers.GetMessageKind() switch
        {
            MessageKind.Reply => true,
            MessageKind.Fault => _includeFaults,
            _ => false
        };

    /// <inheritdoc />
    public override RouteConditionDescription Describe()
        => new("ReplyKind", _includeFaults ? "reply,fault" : "reply", []);
}
