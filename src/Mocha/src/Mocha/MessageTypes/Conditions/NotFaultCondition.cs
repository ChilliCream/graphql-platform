using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Matches messages that are not fault notifications.
/// </summary>
internal sealed class NotFaultCondition : RouteCondition
{
    public static NotFaultCondition Instance { get; } = new();

    private NotFaultCondition() { }

    /// <inheritdoc />
    public override bool Matches(IReceiveContext context)
        => context.Headers.GetMessageKind() != MessageKind.Fault;

    /// <inheritdoc />
    public override RouteConditionDescription Describe()
        => new("NotFault", null, []);
}
