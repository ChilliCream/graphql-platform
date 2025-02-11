using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;

namespace HotChocolate.Fusion.Composition;

internal static class CompositionContextExtensions
{
    /// <summary>
    /// Resets the <see cref="CompositionContext.SupportedBy"/>
    /// field and adds all available subgraph names.
    /// </summary>
    /// <param name="context"></param>
    public static void ResetSupportedBy(
        this CompositionContext context)
    {
        context.SupportedBy.Clear();

        foreach (var subgraph in context.Subgraphs)
        {
            context.SupportedBy.Add(subgraph.Name);
        }
    }

    /// <summary>
    /// Specifies if the provided field reference can be resolved.
    /// </summary>
    /// <param name="context">
    /// The composition context.
    /// </param>
    /// <param name="mutableComplexType"></param>
    /// <param name="fieldRef"></param>
    /// <returns></returns>
    public static bool CanResolveDependency(
        this CompositionContext context,
        MutableComplexTypeDefinition mutableComplexType,
        FieldNode fieldRef)
    {
        return CanResolve(context, mutableComplexType, fieldRef, context.SupportedBy);
    }

    private static bool CanResolve(
        CompositionContext context,
        MutableComplexTypeDefinition mutableComplexType,
        FieldNode fieldRef,
        ISet<string> supportedBy)
    {
        // not supported yet.
        if (fieldRef.Arguments.Count > 0)
        {
            return false;
        }

        if (!mutableComplexType.Fields.TryGetField(fieldRef.Name.Value, out var fieldDef))
        {
            return false;
        }

        if (fieldRef.SelectionSet is not null)
        {
            if (fieldDef.Type.NamedType() is not MutableComplexTypeDefinition namedType)
            {
                return false;
            }

            return CanResolveChildren(context, namedType, fieldRef.SelectionSet, supportedBy);
        }

        supportedBy.IntersectWith(
            fieldDef.Directives
                .Where(t => t.Name.EqualsOrdinal(context.FusionTypes.Source.Name))
                .Select(t => ((StringValueNode)t.Arguments[SubgraphArg]).Value));

        return supportedBy.Count > 0;
    }

    private static bool CanResolveChildren(
        CompositionContext context,
        MutableComplexTypeDefinition mutableComplexType,
        SelectionSetNode selectionSet,
        ISet<string> supportedBy)
    {
        if (selectionSet.Selections.Count != 1)
        {
            return false;
        }

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode fieldNode)
            {
                return CanResolve(context, mutableComplexType, fieldNode, supportedBy);
            }
            else if (selection is InlineFragmentNode inlineFragment)
            {
                if (inlineFragment.TypeCondition is null ||
                    !context.FusionGraph.Types.TryGetType<MutableComplexTypeDefinition>(
                        inlineFragment.TypeCondition.Name.Value,
                        out var fragmentType))
                {
                    return false;
                }

                return CanResolveChildren(
                    context,
                    fragmentType,
                    inlineFragment.SelectionSet,
                    supportedBy);
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
