using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static HotChocolate.Execution.Processing.Plan.QueryPlanSerializationProperties;

namespace HotChocolate.Execution.Processing.Plan;

internal sealed class ResolverNode : QueryPlanNode
{
    private const string _name = "Resolver";
    private const string _strategyProp = "strategy";
    private const string _selectionsProp = "selections";
    private const string _idProp = "id";
    private const string _fieldProp = "field";
    private const string _responseNameProp = "responseName";
    private const string _pureProp = "pure";

    public ResolverNode(
        ISelection first,
        ISelection? firstParent = null,
        ExecutionStrategy? strategy = null)
        : base(strategy ?? QueryPlanBuilder.GetStrategyFromSelection(first))
    {
        First = first;
        FirstParent = firstParent;
        Selections.Add(first);
    }

    public ISelection First { get; }

    public ISelection? FirstParent { get; }

    public List<ISelection> Selections { get; } = new();

    public override ExecutionStep CreateStep()
    {
        var resolver = new ResolverStep(Strategy, Selections);

        return Nodes.Count switch
        {
            0 => resolver,
            1 => new SequenceStep(new[] { resolver, Nodes[0].CreateStep() }),
            _ => new SequenceStep(new ExecutionStep[]
            {
                    resolver,
                    new SequenceStep(Nodes.Select(t => t.CreateStep()).ToArray())
            })
        };
    }

    public override void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString(TypeProp, _name);
        writer.WriteString(_strategyProp, Strategy.ToString());

        writer.WritePropertyName(_selectionsProp);
        writer.WriteStartArray();
        foreach (ISelection? selection in Selections)
        {
            writer.WriteStartObject();
            writer.WriteNumber(_idProp, selection.Id);
            writer.WriteString(_fieldProp, GetFieldFullName(selection));
            writer.WriteString(_responseNameProp, selection.ResponseName.Value);

            if (selection.Strategy is SelectionExecutionStrategy.Pure)
            {
                writer.WriteBoolean(_pureProp, true);
            }

            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        if (Nodes.Count > 0)
        {
            writer.WritePropertyName(NodesProp);
            writer.WriteStartArray();
            foreach (QueryPlanNode? node in Nodes)
            {
                node.Serialize(writer);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    public override object Serialize()
    {
        var selections = new List<object>();

        var serialized = new Dictionary<string, object?>
            {
                { TypeProp, _name },
                { _strategyProp, Strategy.ToString() },
                { _selectionsProp, selections }
            };

        foreach (ISelection? selection in Selections)
        {
            var serializedSelection = new Dictionary<string, object?>
                {
                    { _idProp, selection.Id },
                    { _fieldProp, GetFieldFullName(selection) },
                    { _responseNameProp, selection.ResponseName.Value }
                };

            if (selection.Strategy is SelectionExecutionStrategy.Pure)
            {
                serializedSelection[_pureProp] = true;
            }

            selections.Add(serializedSelection);
        }

        if (Nodes.Count > 0)
        {
            serialized[NodesProp] = Nodes.Select(t => t.Serialize()).ToArray();
        }

        return serialized;
    }

    private static string GetFieldFullName(ISelection selection) =>
        $"{selection.DeclaringType.Name}.{selection.Field.Name}";
}
