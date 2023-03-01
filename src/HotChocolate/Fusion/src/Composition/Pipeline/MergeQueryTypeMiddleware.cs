using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public sealed class MergeQueryTypeMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var schema in context.SubGraphs)
        {
            if (schema.QueryType is not null)
            {
                var queryType = context.FusionGraph.QueryType!;

                if (context.FusionGraph.QueryType is null)
                {
                    queryType = context.FusionGraph.QueryType = new ObjectType("Query");
                    context.FusionGraph.Types.Add(queryType);
                }

                foreach (var field in schema.QueryType.Fields)
                {
                    if (queryType.Fields.TryGetField(field.Name, out var targetField))
                    {
                        context.MergeField(field, targetField);
                    }
                    else
                    {
                        targetField = context.CreateField(field);
                        queryType.Fields.Add(targetField);
                    }

                    var arguments = new List<ArgumentNode>();

                    var selection = new FieldNode(
                        null,
                        new NameNode(field.GetOriginalName()),
                        null,
                        null,
                        Array.Empty<DirectiveNode>(),
                        arguments,
                        null);

                    var selectionSet = new SelectionSetNode(new[] { selection });

                    foreach (var arg in field.Arguments)
                    {
                        arguments.Add(new ArgumentNode(arg.Name, new VariableNode(arg.Name)));
                    }

                    context.ApplyResolvers(targetField, selectionSet, schema.Name);
                }
            }
        }

        if (context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}

static file class MergeEntitiesMiddlewareExtensions
{
    public static void ApplyResolvers(
        this CompositionContext context,
        OutputField field,
        SelectionSetNode selectionSet,
        string schemaName)
    {
        field.Directives.Add(
            CreateResolverDirective(
                context,
                selectionSet,
                schemaName));
    }

    private static Directive CreateResolverDirective(
        CompositionContext context,
        SelectionSetNode selectionSet,
        string schemaName)
        => new Directive(
            context.FusionTypes.Resolver,
            new Argument(DirectiveArguments.Select, selectionSet.ToString(false)),
            new Argument(DirectiveArguments.From, schemaName));
}
