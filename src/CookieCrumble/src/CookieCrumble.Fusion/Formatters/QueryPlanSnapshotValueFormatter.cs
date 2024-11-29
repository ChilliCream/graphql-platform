using System.Buffers;
using CookieCrumble.Formatters;
using HotChocolate.Fusion.Execution.Nodes;

namespace CookieCrumble.Fusion.Formatters;

internal sealed class QueryPlanSnapshotValueFormatter() : SnapshotValueFormatter<QueryPlan>("json")
{
    protected override void Format(IBufferWriter<byte> snapshot, QueryPlan value)
        => value.Format(snapshot);
}
