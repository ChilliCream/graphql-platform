using System.Collections.Generic;
using System.Text.Json;

namespace HotChocolate.Execution.Processing.Plan
{
    internal class OperationQueryPlanNode : QueryPlanNode
    {
        public OperationQueryPlanNode(QueryPlanNode operation)
            : base(ExecutionStrategy.Serial)
        {
            Operation = operation;
            AddNode(operation);
        }

        public QueryPlanNode Operation { get; }

        public List<QueryPlanNode> Deferred { get; } = new();

        public override QueryPlanStep CreateStep() =>
            Operation.CreateStep();

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "operation");

            writer.WritePropertyName("root");
            Operation.Serialize(writer);

            if (Deferred.Count > 0)
            {
                writer.WritePropertyName("deferred");
                writer.WriteStartArray();
                foreach (var node in Deferred)
                {
                    node.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}
