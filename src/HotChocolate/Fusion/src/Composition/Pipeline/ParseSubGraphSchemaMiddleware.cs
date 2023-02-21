using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion.Composition;

public sealed class ParseSubGraphSchemaMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var config in context.Configurations)
        {
            var schema = SchemaParser.Parse(config.Schema);
            schema.Name = config.Name;
            context.SubGraphs.Add(schema);
        }

        await next(context).ConfigureAwait(false);
    }
}
