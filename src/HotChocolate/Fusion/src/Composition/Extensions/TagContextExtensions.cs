namespace HotChocolate.Fusion.Composition;

internal static class TagContextExtensions
{
    public static TagContext GetTagContext(this CompositionContext context)
    {
        const string key = "HotChocolate.Fusion.Composition.TagContext";

        if(context.ContextData.TryGetValue(key, out var value) &&
            value is TagContext tagContext)
        {
            return tagContext;
        }

        tagContext = new TagContext();
        context.ContextData[key] = tagContext;
        return tagContext;
    }
}
