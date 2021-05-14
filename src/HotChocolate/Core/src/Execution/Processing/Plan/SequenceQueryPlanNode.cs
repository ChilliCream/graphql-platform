using System.Linq;
using System.Text.Json;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class SequenceQueryPlanNode : QueryPlanNode
    {
        public SequenceQueryPlanNode() : base(ExecutionStrategy.Serial)
        {
        }

        public override QueryPlanStep CreateStep()
        {
            return new SequenceQueryPlanStep(Nodes.Select(t => t.CreateStep()).ToArray());
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "Sequence");

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

        public static SequenceQueryPlanNode Create(params QueryPlanNode[] nodes)
        {
            var sequence = new SequenceQueryPlanNode();
            foreach (QueryPlanNode node in nodes)
            {
                sequence.AddNode(node);
            }
            return sequence;
        }
    }
}
