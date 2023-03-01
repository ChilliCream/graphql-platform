namespace HotChocolate.Fusion.Composition.Pipeline;

public interface IMergeMiddleware
{
    ValueTask InvokeAsync(CompositionContext context, MergeDelegate next);
}
