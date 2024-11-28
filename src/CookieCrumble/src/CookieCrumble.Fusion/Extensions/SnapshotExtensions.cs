using CookieCrumble.Fusion.Formatters;

namespace CookieCrumble.Fusion;

public static class SnapshotExtensions
{
    public static void TryRegisterFusionFormatters(this Snapshot _)
    {
        Snapshot.TryRegisterFormatter(SnapshotValueFormatters.QueryPlan);
    }
}
