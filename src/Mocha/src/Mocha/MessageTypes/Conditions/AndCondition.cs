using System.Collections.Immutable;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Matches only when all of its child conditions match.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AndCondition"/> class.
/// </remarks>
/// <param name="conditions">The child conditions that must all match.</param>
internal sealed class AndCondition(ImmutableArray<RouteCondition> conditions) : RouteCondition
{
    /// <inheritdoc />
    public override void Initialize(IMessagingConfigurationContext context)
    {
        foreach (var condition in conditions)
        {
            condition.Initialize(context);
        }
    }

    /// <inheritdoc />
    public override bool Matches(IReceiveContext context)
    {
        foreach (var condition in conditions)
        {
            if (!condition.Matches(context))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override RouteConditionDescription Describe()
        => new("And", null, [.. conditions.Select(static c => c.Describe())]);

    public static AndCondition Create(params ReadOnlySpan<RouteCondition> conditions) => new([.. conditions]);
}
