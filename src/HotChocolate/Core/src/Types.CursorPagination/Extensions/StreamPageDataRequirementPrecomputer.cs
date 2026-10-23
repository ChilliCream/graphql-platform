using HotChocolate.Execution.Processing;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Stores the data requirements of a connection selection on the selection when none of the
/// selections the requirements depend on carries a condition.
/// </summary>
internal sealed class StreamPageDataRequirementPrecomputer : ISelectionSetHandler
{
    private static readonly object[] s_boxedRequirements = CreateBoxedRequirements();

    private StreamPageDataRequirementPrecomputer()
    {
    }

    public static StreamPageDataRequirementPrecomputer Instance { get; } = new();

    public void OnSelectionSetCompiled(Selection selection, SelectionSet selectionSet)
    {
        if (TryPrecompute(selection, selectionSet, out var requirements))
        {
            var features = selection.Features;
            features[typeof(StreamPageDataRequirements)] = s_boxedRequirements[(int)requirements];
        }
    }

    private static bool TryPrecompute(
        Selection connection,
        SelectionSet selectionSet,
        out StreamPageDataRequirements requirements)
    {
        requirements = StreamPageDataRequirements.None;

        foreach (var child in selectionSet.Selections)
        {
            if (child.Field.Name.Equals(ConnectionType.Names.PageInfo, StringComparison.Ordinal))
            {
                if (!TryAddPageInfo(connection, child, ref requirements))
                {
                    return false;
                }

                continue;
            }

            var childRequirements = StreamPageDataRequirementAnalyzer.GetConnectionRequirements(child.Field.Name);

            if (childRequirements is StreamPageDataRequirements.None)
            {
                continue;
            }

            if (!HasNoOwnConditions(connection, child))
            {
                return false;
            }

            requirements |= childRequirements;
        }

        return true;
    }

    private static bool TryAddPageInfo(
        Selection connection,
        Selection pageInfo,
        ref StreamPageDataRequirements requirements)
    {
        if (!HasNoOwnConditions(connection, pageInfo))
        {
            return false;
        }

        if (pageInfo.Type.NamedType() is not ObjectType pageInfoType)
        {
            return true;
        }

        foreach (var child in pageInfo.GetSelectionSet(pageInfoType).Selections)
        {
            var childRequirements = StreamPageDataRequirementAnalyzer.GetPageInfoRequirements(child.Field.Name);

            if (childRequirements is StreamPageDataRequirements.None)
            {
                continue;
            }

            if (!HasNoOwnConditions(connection, child))
            {
                return false;
            }

            requirements |= childRequirements;
        }

        return true;
    }

    private static bool HasNoOwnConditions(Selection connection, Selection child)
    {
        if (!child.HasSameConditions(connection))
        {
            return false;
        }

        foreach (var syntaxNode in child.SyntaxNodes)
        {
            foreach (var directive in syntaxNode.Node.Directives)
            {
                if (directive.Name.Value.Equals(DirectiveNames.Stream.Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static object[] CreateBoxedRequirements()
    {
        var boxed = new object[(int)(StreamPageDataRequirements.TotalCount
            | StreamPageDataRequirements.FirstRow
            | StreamPageDataRequirements.AllRows) + 1];

        for (var i = 0; i < boxed.Length; i++)
        {
            boxed[i] = (StreamPageDataRequirements)i;
        }

        return boxed;
    }
}
