using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination;

internal static class StreamPageDataRequirementAnalyzer
{
    private const string ForwardCursors = "forwardCursors";
    private const string BackwardCursors = "backwardCursors";

    public static StreamPageDataRequirements Analyze(IResolverContext context)
    {
        var selection = context.Selection;

        if (selection.Type.NamedType() is not ObjectType connectionType)
        {
            return StreamPageDataRequirements.None;
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

            switch (child.Field.Name)
            {
                case ConnectionType.Names.Edges:
                case ConnectionType.Names.Nodes:
                    requirements |= StreamPageDataRequirements.AllRows;
                    break;

                case ConnectionType.Names.TotalCount:
                    requirements |= StreamPageDataRequirements.TotalCount;
                    break;

                case ConnectionType.Names.PageInfo:
                    requirements |= AnalyzePageInfo(context, child, deferFlags, connectionDeferUsage);
                    break;
            }
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

            switch (child.Field.Name)
            {
                case PageInfo.Names.HasPreviousPage:
                case PageInfo.Names.StartCursor:
                    requirements |= StreamPageDataRequirements.FirstRow;
                    break;

                case PageInfo.Names.HasNextPage:
                case PageInfo.Names.EndCursor:
                case ForwardCursors:
                case BackwardCursors:
                    requirements |= StreamPageDataRequirements.AllRows;
                    break;
            }
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

    private static bool IsStreamEnabled(DirectiveNode directive, IVariableValueCollection variables)
    {
        var value = directive.GetArgumentValue(DirectiveNames.Stream.Arguments.If);

        return value switch
        {
            null => true,
            BooleanValueNode literal => literal.Value,
            VariableNode variable => !variables.TryGetValue<BooleanValueNode>(variable.Name.Value, out var coerced)
                || coerced.Value,
            _ => true
        };
    }
}
