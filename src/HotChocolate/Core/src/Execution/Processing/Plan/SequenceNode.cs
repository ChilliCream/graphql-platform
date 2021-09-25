using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static HotChocolate.Execution.Processing.Plan.QueryPlanSerializationProperties;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class SequenceNode : QueryPlanNode
    {
        private const string _name = "Sequence";

        public SequenceNode() : base(ExecutionStrategy.Serial)
        {
        }

        public bool CancelOnError { get; set; }

        public override ExecutionStep CreateStep()
            => new SequenceStep(CreateSteps(Nodes), CancelOnError);

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString(TypeProp, _name);

            if (CancelOnError)
            {
                writer.WriteBoolean(CancelOnErrorProp, true);
            }

            if (Nodes.Count > 0)
            {
                writer.WritePropertyName(NodesProp);
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
            var props = new Dictionary<string, object?>
            {
                { TypeProp, _name },
                { NodesProp, Nodes.Select(t => t.Serialize()).ToArray() }
            };

            if (CancelOnError)
            {
                props.Add(CancelOnErrorProp, true);
            }

            return props;
        }

        public static SequenceNode Create(params QueryPlanNode[] nodes)
        {
            var sequence = new SequenceNode();

            foreach (QueryPlanNode node in nodes)
            {
                sequence.AddNode(node);
            }

            return sequence;
        }
    }
}
