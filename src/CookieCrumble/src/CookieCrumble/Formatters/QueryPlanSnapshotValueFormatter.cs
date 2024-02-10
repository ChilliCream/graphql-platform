#if NET7_0_OR_GREATER
using System.Buffers;
using HotChocolate.Fusion.Execution.Nodes;

namespace CookieCrumble.Formatters;

internal sealed class QueryPlanSnapshotValueFormatter : SnapshotValueFormatter<QueryPlan>
{
    protected override void Format(IBufferWriter<byte> snapshot, QueryPlan value)
    {
        value.Format(snapshot);
    }
}
#endif
