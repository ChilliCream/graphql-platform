using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static HotChocolate.Execution.Processing.Plan.QueryPlanSerializationProperties;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class SequenceQueryPlanNode : QueryPlanNode
    {
        private const string _name = "Sequence";

        public SequenceQueryPlanNode() : base(ExecutionStrategy.Serial)
        {
        }

        public override QueryPlanStep CreateStep() =>
            new SequenceQueryPlanStep(Nodes.Select(t => t.CreateStep()).ToArray());

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

        public override object Serialize()
        {
            return new Dictionary<string, object?>
            {
                { TypeProp, _name },
                { NodesProp, Nodes.Select(t => t.Serialize()).ToArray() }
            };
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
