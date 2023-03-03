namespace HotChocolate.Fusion.Composition.Pipeline;

internal  interface IMergeMiddleware
{
    ValueTask InvokeAsync(CompositionContext context, MergeDelegate next);
}
