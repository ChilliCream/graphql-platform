using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Never matches. Assigned to the reply route that has no message type so the generic receive router
/// has a non-null condition to evaluate while keeping the route inert.
/// </summary>
internal sealed class NoMatchCondition : RouteCondition
{
    private NoMatchCondition()
    {
    }

    /// <inheritdoc />
    public override bool Matches(IReceiveContext context) => false;

    /// <inheritdoc />
    public override RouteConditionDescription Describe()
        => new("NoMatch", null, []);

    /// <summary>
    /// Gets the shared instance of the <see cref="NoMatchCondition"/>.
    /// </summary>
    public static NoMatchCondition Instance { get; } = new();
}
