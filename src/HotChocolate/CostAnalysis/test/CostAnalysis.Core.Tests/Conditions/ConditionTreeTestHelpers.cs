using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Shared helpers for condition-tree extraction tests: building a snapshot
/// from SDL and rendering a tree into a deterministic, human-readable dump
/// for snapshot assertions.
/// </summary>
internal static class ConditionTreeTestHelpers
{
    public static CostSchemaSnapshot BuildSnapshot(string sdl)
    {
        var schema = SchemaParser.Parse(sdl);
        return CostSchemaSnapshot.Create(schema, new CostEngineOptions());
    }

    public static OperationDefinitionNode ParseOperation(DocumentNode document)
        => document.Definitions.OfType<OperationDefinitionNode>().Single();

    /// <summary>
    /// Renders every node of <paramref name="tree"/>, in arena order, as
    /// <c>[possible types] (boolean literals) responseName:occurrenceCount ...</c>,
    /// one line per node, the root marked with a leading <c>*</c>.
    /// </summary>
    public static string Dump(CostSchemaSnapshot snapshot, ConditionTree tree, params string[] objectTypeNames)
    {
        var lines = new List<string>(tree.Nodes.Count);

        for (var i = 0; i < tree.Nodes.Count; i++)
        {
            var node = tree.Nodes[i];
            var line = new StringBuilder();
            line.Append(i == tree.RootNodeId ? '*' : ' ');
            line.Append('[').Append(DescribeTypes(snapshot, node.Condition.PossibleTypes, objectTypeNames)).Append(']');
            line.Append(" (").Append(string.Join(",", node.Condition.BooleanCondition)).Append(')');

            foreach (var group in node.FieldGroups)
            {
                line.Append(' ').Append(group.ResponseName).Append(':').Append(group.Fields.Count);
            }

            lines.Add(line.ToString());
        }

        return string.Join('\n', lines);
    }

    private static string DescribeTypes(CostSchemaSnapshot snapshot, PossibleTypeSet types, string[] objectTypeNames)
        => string.Join(",", objectTypeNames.Where(name => types.Contains(snapshot.GetObjectTypeIndex(name))));
}
