using System.Buffers;
using HotChocolate.Fusion.Planning;

namespace CookieCrumble.Formatters;

internal sealed class QueryPlanSnapshotValueFormatter : SnapshotValueFormatter<QueryPlan>
{
    protected override void Format(IBufferWriter<byte> snapshot, QueryPlan value)
    {
        value.Format(snapshot);
    }
}
