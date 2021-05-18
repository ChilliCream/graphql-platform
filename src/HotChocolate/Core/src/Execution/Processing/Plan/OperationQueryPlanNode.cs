using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static HotChocolate.Execution.Processing.Plan.QueryPlanSerializationProperties;

namespace HotChocolate.Execution.Processing.Plan
{
    internal class OperationQueryPlanNode : QueryPlanNode
    {
        private const string _name = "Operation";
        private const string _rootProp = "root";
        private const string _deferredProp = "deferred";

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
            writer.WriteString(TypeProp, _name);

            writer.WritePropertyName(_rootProp);
            Operation.Serialize(writer);

            if (Deferred.Count > 0)
            {
                writer.WritePropertyName(_deferredProp);
                writer.WriteStartArray();
                foreach (var node in Deferred)
                {
                    node.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        public override object Serialize()
        {
            var serialized = new Dictionary<string, object?>
            {
                { TypeProp, _name },
                { _rootProp, Operation.Serialize() }
            };

            if (Deferred.Count > 0)
            {
                 serialized[_deferredProp] = Deferred.Select(t => t.Serialize()).ToArray();
            }

            return serialized;
        }
    }
}
