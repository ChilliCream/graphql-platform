using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class ResolverQueryPlanNode : QueryPlanNode
    {
        public ResolverQueryPlanNode(ISelection first, ISelection? firstParent = null)
            : base(QueryPlanBuilder.GetStrategyFromSelection(first))
        {
            First = first;
            FirstParent = firstParent;
            Selections.Add(first);
        }

        public ISelection? FirstParent { get; }

        public ISelection First { get; }

        public List<ISelection> Selections { get; } = new();

        public override QueryPlanStep CreateStep()
        {
            var selectionStep = new ResolverQueryPlanStep(Strategy, Selections);

            if (Nodes.Count == 0)
            {
                return selectionStep;
            }

            if (Nodes.Count == 1)
            {
                return new SequenceQueryPlanStep(
                    new[]
                    {
                        selectionStep,
                        Nodes[0].CreateStep()
                    });
            }

            return new SequenceQueryPlanStep(
                new QueryPlanStep[]
                {
                    selectionStep,
                    new SequenceQueryPlanStep(Nodes.Select(t => t.CreateStep()).ToArray())
                });
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "Resolver");
            writer.WriteString("strategy", Strategy.ToString());

            writer.WritePropertyName("selections");
            writer.WriteStartArray();
            foreach (var selection in Selections)
            {
                writer.WriteStartObject();
                writer.WriteNumber("id", selection.Id);
                writer.WriteString("field", $"{selection.DeclaringType.Name}.{selection.Field.Name}");
                writer.WriteString("responseName", selection.ResponseName);

                if (selection.Strategy == SelectionExecutionStrategy.Pure)
                {
                    writer.WriteBoolean("pure", true);
                }

                if (selection.Strategy == SelectionExecutionStrategy.Inline)
                {
                    writer.WriteBoolean("inline", true);
                }

                writer.WriteEndObject();
            }
            writer.WriteEndArray();

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
    }
}
