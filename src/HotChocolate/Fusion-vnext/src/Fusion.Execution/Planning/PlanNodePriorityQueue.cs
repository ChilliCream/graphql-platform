using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Planning;

internal sealed class PlanNodePriorityQueue
{
    private readonly PriorityQueue<PlanNode, PlanNodePriority> _queue = new();
    private int _sequence;

    public int Count => _queue.Count;

    public uint ExploredPlans { get; private set; }

    public void Enqueue(PlanNode node, double resolutionCost = 0)
    {
        // Our stated goal is to find the plan with the least amount of requests (operations)
        // and the shortest critical path length (waterfall latency).
        var objective = new PlanSearchObjective(
            node.OperationStepCount,
            node.CriticalPathLength);

        var priority = new PlanNodePriority(
            // Primary objective
            objective,
            // An estimate of how much work it will take to complete this plan
            node.BacklogCost,
            // Steering for when objective and backlog cost are equal.
            resolutionCost,
            // Tie breaker
            _sequence++);

        _queue.Enqueue(node, priority);
    }

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
        PlanSearchObjective Objective,
        double BacklogCost,
        double ResolutionCost,
        long Sequence) : IComparable<PlanNodePriority>
    {
        public int CompareTo(PlanNodePriority other)
        {
            var result = Objective.CompareTo(other.Objective);
            if (result != 0)
            {
                return result;
            }

            result = BacklogCost.CompareTo(other.BacklogCost);
            if (result != 0)
            {
                return result;
            }

            result = ResolutionCost.CompareTo(other.ResolutionCost);
            if (result != 0)
            {
                return result;
            }

            return Sequence.CompareTo(other.Sequence);
        }
    }

    private readonly record struct PlanSearchObjective(
        int OperationStepCount,
        int CriticalPathLength)
        : IComparable<PlanSearchObjective>
    {
        public int CompareTo(PlanSearchObjective other)
        {
            var result = OperationStepCount.CompareTo(other.OperationStepCount);
            if (result != 0)
            {
                return result;
            }

            return CriticalPathLength.CompareTo(other.CriticalPathLength);
        }
    }
}
