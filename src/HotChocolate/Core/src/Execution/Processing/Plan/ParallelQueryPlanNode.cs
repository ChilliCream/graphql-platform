using System.Linq;
using System.Text.Json;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class ParallelQueryPlanNode : QueryPlanNode
    {
        public ParallelQueryPlanNode() : base(ExecutionStrategy.Parallel)
        {
        }

        public override QueryPlanStep CreateStep() =>
            new ParallelQueryPlanStep(Nodes.Select(t => t.CreateStep()).ToArray());

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "Parallel");

            if (Nodes.Count > 0)
            {
                writer.WritePropertyName("nodes");
                writer.WriteStartArray();
                foreach (var node in Nodes)
                {
                    node.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        public static ParallelQueryPlanNode Create(params QueryPlanNode[] nodes)
        {
            var parallel = new ParallelQueryPlanNode();
            foreach (QueryPlanNode node in nodes)
            {
                parallel.AddNode(node);
            }
            return parallel;
        }
    }
}
