using CookieCrumble.Formatters;

namespace CookieCrumble.Fusion.Formatters;

/// <summary>
/// Provides access to well-known snapshot value formatters.
/// </summary>
public static class SnapshotValueFormatters
{
    public static ISnapshotValueFormatter QueryPlan { get; } =
        new QueryPlanSnapshotValueFormatter();
}
