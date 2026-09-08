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
    /// <c>[possible types] (boolean literals) responseName:occurrenceCount ... branches=label->targetId,...</c>,
    /// one line per node, the root marked with a leading <c>*</c>. The
    /// <c>branches</c> segment is omitted for a node with no outgoing edges.
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

            if (node.Branches.Count > 0)
            {
                line.Append(" branches=");
                line.Append(string.Join(",", node.Branches.Select(branch => $"{DescribeBranch(branch.Condition)}->{branch.TargetNodeId}")));
            }

            lines.Add(line.ToString());
        }

        return string.Join('\n', lines);
    }

    private static string DescribeTypes(CostSchemaSnapshot snapshot, PossibleTypeSet types, string[] objectTypeNames)
        => string.Join(",", objectTypeNames.Where(name => types.Contains(snapshot.GetObjectTypeIndex(name))));

    private static string DescribeBranch(BranchCondition condition)
        => condition.TypeName ?? condition.Literal.ToString()!;
}
