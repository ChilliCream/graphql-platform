using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static HotChocolate.Execution.Processing.Plan.QueryPlanSerializationProperties;

namespace HotChocolate.Execution.Processing.Plan
{
    internal class OperationNode : QueryPlanNode
    {
        private const string _name = "Operation";
        private const string _rootProp = "root";
        private const string _deferredProp = "deferred";
        private const string _streamsProp = "streams";

        public OperationNode(QueryPlanNode operation)
            : base(ExecutionStrategy.Serial)
        {
            Operation = operation;
            AddNode(operation);
        }

        public QueryPlanNode Operation { get; }

        public List<QueryPlanNode> Deferred { get; } = new();

        public List<StreamPlanNode> Streams { get; } = new();

        public override ExecutionStep CreateStep() =>
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

            if (Streams.Count > 0)
            {
                writer.WritePropertyName(_streamsProp);
                writer.WriteStartArray();
                foreach (StreamPlanNode node in Streams.OrderBy(t => t.Id))
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
                 serialized[_deferredProp] =
                     Deferred.Select(t => t.Serialize()).ToArray();
            }

            if (Streams.Count > 0)
            {
                serialized[_streamsProp] =
                    Streams.OrderBy(t => t.Id).Select(t => t.Serialize()).ToArray();
            }

            return serialized;
        }
    }
}
