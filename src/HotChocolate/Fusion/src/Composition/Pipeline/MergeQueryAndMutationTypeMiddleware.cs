using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class MergeQueryAndMutationTypeMiddleware : IMergeMiddleware
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

                MergeRootFields(context, schema, schema.QueryType, queryType);
            }

            if (schema.MutationType is not null)
            {
                var queryType = context.FusionGraph.MutationType!;

                if (context.FusionGraph.MutationType is null)
                {
                    queryType = context.FusionGraph.MutationType = new ObjectType("Mutation");
                    context.FusionGraph.Types.Add(queryType);
                }

                MergeRootFields(context, schema, schema.MutationType, queryType);
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }

    private static void MergeRootFields(
        CompositionContext context,
        Schema sourceSchema,
        ObjectType sourceRootType,
        ObjectType targetRootType)
    {
        foreach (var field in sourceRootType.Fields)
        {
            if (targetRootType.Fields.TryGetField(field.Name, out var targetField))
            {
                context.MergeField(field, targetField, targetRootType.Name);
            }
            else
            {
                targetField = context.CreateField(field, context.FusionGraph);
                targetRootType.Fields.Add(targetField);
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
                context.ApplyVariable(targetField, arg, sourceSchema.Name);
            }

            context.ApplyResolvers(targetField, selectionSet, sourceSchema.Name);
        }
    }
}

static file class MergeQueryTypeMiddlewareExtensions
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
