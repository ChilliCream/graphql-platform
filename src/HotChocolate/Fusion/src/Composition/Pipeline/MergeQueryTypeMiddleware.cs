using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class MergeQueryTypeMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var schema in context.Subgraphs)
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
                        context.MergeField(field, targetField, queryType.Name);
                    }
                    else
                    {
                        targetField = context.CreateField(field, context.FusionGraph);
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
                        context.ApplyVariable(targetField, arg, schema.Name);
                    }

                    context.ApplyResolvers(targetField, selectionSet, schema.Name);
                }
            }
        }

        if (!context.Log.HasErrors)
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
        string subgraphName)
    {
        Dictionary<string, ITypeNode>? arguments = null;

        foreach (var argument in field.Arguments)
        {
            arguments ??= new Dictionary<string, ITypeNode>();
            arguments.Add(argument.Name, argument.Type.ToTypeNode());
        }

        field.Directives.Add(
            CreateResolverDirective(
                context,
                selectionSet,
                subgraphName,
                arguments));
    }

    public static void ApplyVariable(
        this CompositionContext context,
        OutputField field,
        InputField argument,
        string subgraphName)
    {
        field.Directives.Add(
            CreateVariableDirective(
                context,
                argument.Name,
                subgraphName));
    }

    private static Directive CreateResolverDirective(
        CompositionContext context,
        SelectionSetNode selectionSet,
        string subgraphName,
        Dictionary<string, ITypeNode>? arguments = null)
        => context.FusionTypes.CreateResolverDirective(
            subgraphName,
            selectionSet,
            arguments);

    private static Directive CreateVariableDirective(
        CompositionContext context,
        string variableName,
        string subgraphName)
        => context.FusionTypes.CreateVariableDirective(
            subgraphName,
            variableName,
            variableName);
}
