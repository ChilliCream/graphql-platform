using CookieCrumble.Fusion.Formatters;

namespace CookieCrumble.Fusion;

public static class CookieCrumbleFusion
{
    public static void Initialize()
    {
        Snapshot.TryRegisterFormatter(SnapshotValueFormatters.QueryPlan);
    }
}
