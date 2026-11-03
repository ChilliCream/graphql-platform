using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination;

internal static class StreamPageDataRequirementAnalyzer
{
    private const string ForwardCursors = "forwardCursors";
    private const string BackwardCursors = "backwardCursors";

    /// <summary>
    /// Gets the requirements of a connection field other than <c>pageInfo</c>.
    /// </summary>
    public static StreamPageDataRequirements GetConnectionRequirements(string fieldName)
        => fieldName switch
        {
            ConnectionType.Names.Edges or ConnectionType.Names.Nodes => StreamPageDataRequirements.AllRows,
            ConnectionType.Names.TotalCount => StreamPageDataRequirements.TotalCount,
            _ => StreamPageDataRequirements.None
        };

    /// <summary>
    /// Gets the requirements of a <c>pageInfo</c> field.
    /// </summary>
    public static StreamPageDataRequirements GetPageInfoRequirements(string fieldName)
        => fieldName switch
        {
            PageInfo.Names.HasPreviousPage or PageInfo.Names.StartCursor => StreamPageDataRequirements.FirstRow,
            PageInfo.Names.HasNextPage or PageInfo.Names.EndCursor or ForwardCursors or BackwardCursors
                => StreamPageDataRequirements.AllRows,
            _ => StreamPageDataRequirements.None
        };

    public static StreamPageDataRequirements Analyze(IResolverContext context)
    {
        var selection = context.Selection;

        if (selection.Type.NamedType() is not ObjectType connectionType)
        {
            return StreamPageDataRequirements.None;
        }

        if (selection.Features.TryGet<StreamPageDataRequirements>(out var precomputed))
        {
            return precomputed;
        }

        // The requirements of an unconditional selection are stored while its selection set is compiled.
        selection.GetSelectionSet(connectionType);

        if (selection.Features.TryGet(out precomputed))
        {
            return precomputed;
        }

        var deferFlags = context.Operation.CreateDeferConditionFlags(context.Variables);
        var connectionDeferUsage = selection.GetPrimaryDeferUsage(deferFlags);
        var requirements = StreamPageDataRequirements.None;

        foreach (var child in context.GetSelections(connectionType))
        {
            if (IsDeferredOrStreamed(child, context.Variables, deferFlags, connectionDeferUsage))
            {
                continue;
            }

            requirements |= child.Field.Name.Equals(ConnectionType.Names.PageInfo, StringComparison.Ordinal)
                ? AnalyzePageInfo(context, child, deferFlags, connectionDeferUsage)
                : GetConnectionRequirements(child.Field.Name);
        }

        return requirements;
    }

    private static StreamPageDataRequirements AnalyzePageInfo(
        IResolverContext context,
        Selection pageInfo,
        ConditionFlags deferFlags,
        DeferUsage? connectionDeferUsage)
    {
        if (pageInfo.Type.NamedType() is not ObjectType pageInfoType)
        {
            return StreamPageDataRequirements.None;
        }

        var requirements = StreamPageDataRequirements.None;

        foreach (var child in context.GetSelections(pageInfoType, pageInfo))
        {
            if (IsDeferredOrStreamed(child, context.Variables, deferFlags, connectionDeferUsage))
            {
                continue;
            }

            requirements |= GetPageInfoRequirements(child.Field.Name);
        }

        return requirements;
    }

    private static bool IsDeferredOrStreamed(
        Selection selection,
        IVariableValueCollection variables,
        ConditionFlags deferFlags,
        DeferUsage? connectionDeferUsage)
    {
        if (selection.GetPrimaryDeferUsage(deferFlags) != connectionDeferUsage)
        {
            return true;
        }

        foreach (var syntaxNode in selection.SyntaxNodes)
        {
            foreach (var directive in syntaxNode.Node.Directives)
            {
                if (directive.Name.Value.Equals(DirectiveNames.Stream.Name, StringComparison.Ordinal)
                    && IsStreamEnabled(directive, variables))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool IsStreamEnabled(DirectiveNode directive, IVariableValueCollection variables)
        => DirectiveIfCondition.IsEnabled(
            DirectiveNames.Stream.Name,
            directive.GetArgumentValue(DirectiveNames.Stream.Arguments.If),
            variables,
            defaultValue: true);
}
