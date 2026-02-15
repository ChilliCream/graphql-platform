using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Planning;

internal sealed class PlanNodePriorityQueue
{
    private readonly PriorityQueue<PlanNode, PlanNodePriority> _queue = new();
    private int _sequence;

    public int Count => _queue.Count;

    public uint ExploredPlans { get; private set; }

    public void Enqueue(PlanNode node)
        => _queue.Enqueue(node, new PlanNodePriority(node.TotalCost, _sequence++));

    public bool TryDequeue([NotNullWhen(true)] out PlanNode? node)
    {
        if (_queue.TryDequeue(out node, out _))
        {
            ExploredPlans++;
            return true;
        }

        return false;
    }

    private readonly record struct PlanNodePriority(
        double TotalCost,
        long Sequence)
        : IComparable<PlanNodePriority>
    {
        public int CompareTo(PlanNodePriority other)
        {
            var result = TotalCost.CompareTo(other.TotalCost);
            if (result != 0)
            {
                return result;
            }

            return Sequence.CompareTo(other.Sequence);
        }
    }
}
