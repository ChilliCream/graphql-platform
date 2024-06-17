namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// A middleware component that removes the internal fusion type declarations from
/// the distributed fusion graph.
/// </summary>
internal sealed class RemoveFusionTypesMiddleware : IMergeMiddleware
{
    public ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        // Remove the fusion types from the GraphQL schema
        context.FusionGraph.Types.Remove(context.FusionTypes.Type);
        context.FusionGraph.Types.Remove(context.FusionTypes.TypeName);
        context.FusionGraph.Types.Remove(context.FusionTypes.Selection);
        context.FusionGraph.Types.Remove(context.FusionTypes.SelectionSet);
        context.FusionGraph.Types.Remove(context.FusionTypes.Uri);
        context.FusionGraph.Types.Remove(context.FusionTypes.ArgumentDefinition);
        context.FusionGraph.Types.Remove(context.FusionTypes.ResolverKind);

        // Remove the fusion directives from the GraphQL schema
        context.FusionGraph.DirectiveDefinitions.Remove(context.FusionTypes.Resolver);
        context.FusionGraph.DirectiveDefinitions.Remove(context.FusionTypes.Variable);
        context.FusionGraph.DirectiveDefinitions.Remove(context.FusionTypes.Source);
        context.FusionGraph.DirectiveDefinitions.Remove(context.FusionTypes.Node);
        context.FusionGraph.DirectiveDefinitions.Remove(context.FusionTypes.ReEncodeId);
        context.FusionGraph.DirectiveDefinitions.Remove(context.FusionTypes.Transport);
        context.FusionGraph.DirectiveDefinitions.Remove(context.FusionTypes.Fusion);

        return next(context);
    }
}
