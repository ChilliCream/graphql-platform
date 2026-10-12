using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Checks that every source schema operation of a plan satisfies the FieldsInSetCanMerge rule,
/// without type information: fields that share a response name must select the same field with
/// the same arguments, and their child selections must merge in turn.
/// </summary>
internal static class SourceOperationFieldMergeAssert
{
    public static void AllOperationsCanMerge(OperationPlan plan)
    {
        var violations = new List<string>();

        foreach (var node in plan.AllNodes.OfType<OperationExecutionNode>())
        {
            var document = Utf8GraphQLParser.Parse(node.Operation.Value.Span);
            var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
            CollectViolations(operation.SelectionSet.Selections, node.Operation.Name, violations);
        }

        Assert.Empty(violations);
    }

    private static void CollectViolations(
        IReadOnlyList<ISelectionNode> selections,
        string path,
        List<string> violations)
    {
        var groups = new Dictionary<string, List<FieldNode>>(StringComparer.Ordinal);
        CollectFields(selections, groups);

        foreach (var (responseName, fields) in groups)
        {
            var first = fields[0];
            var fieldPath = $"{path}.{responseName}";

            for (var i = 1; i < fields.Count; i++)
            {
                var other = fields[i];

                if (!first.Name.Value.Equals(other.Name.Value, StringComparison.Ordinal)
                    || !first.Arguments.SequenceEqual(other.Arguments, SyntaxComparer.BySyntax))
                {
                    violations.Add($"{fieldPath}: {first} conflicts with {other}");
                }
            }

            CollectViolations(
                fields
                    .Where(t => t.SelectionSet is not null)
                    .SelectMany(t => t.SelectionSet!.Selections)
                    .ToArray(),
                fieldPath,
                violations);
        }
    }

    private static void CollectFields(
        IReadOnlyList<ISelectionNode> selections,
        Dictionary<string, List<FieldNode>> groups)
    {
        foreach (var selection in selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    var responseName = field.Alias?.Value ?? field.Name.Value;

                    if (!groups.TryGetValue(responseName, out var group))
                    {
                        group = [];
                        groups.Add(responseName, group);
                    }

                    group.Add(field);
                    break;

                case InlineFragmentNode fragment:
                    CollectFields(fragment.SelectionSet.Selections, groups);
                    break;
            }
        }
    }
}
