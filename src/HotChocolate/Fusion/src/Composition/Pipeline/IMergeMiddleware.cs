namespace HotChocolate.Fusion.Composition;

public interface IMergeMiddleware
{
    ValueTask InvokeAsync(CompositionContext context, MergeDelegate next);
}
