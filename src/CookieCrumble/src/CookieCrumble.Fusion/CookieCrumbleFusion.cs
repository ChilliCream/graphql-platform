using CookieCrumble.Formatters;
using SnapshotValueFormatters = CookieCrumble.Fusion.Formatters.SnapshotValueFormatters;

namespace CookieCrumble.Fusion;

public class CookieCrumbleFusion : SnapshotModule
{
    protected override IEnumerable<ISnapshotValueFormatter> CreateFormatters()
    {
        yield return SnapshotValueFormatters.QueryPlan;
    }
}
