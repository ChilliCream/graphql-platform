using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class MergeQueryAndMutationTypeMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var skipOnQuery = new HashSet<string>(StringComparer.Ordinal);
        var skipOnMutation = new HashSet<string>(StringComparer.Ordinal);

        if (!context.Features.IsNodeFieldSupported())
        {
            skipOnQuery.Add("node");
            skipOnQuery.Add("nodes");
        }

        foreach (var schema in context.Subgraphs)
        {
            if (schema.QueryType is not null)
            {
                var targetType = context.FusionGraph.QueryType!;

                if (context.FusionGraph.QueryType is null)
                {
                    targetType = context.FusionGraph.QueryType = new ObjectType(schema.QueryType.Name);
                    targetType.MergeDescriptionWith(schema.QueryType);
                    context.FusionGraph.Types.Add(targetType);
                }

                MergeRootFields(
                    context, 
                    schema, 
                    schema.QueryType, 
                    targetType, 
                    OperationType.Query, 
                    skipOnQuery);
            }

            if (schema.MutationType is not null)
            {
                var targetType = context.FusionGraph.MutationType!;

                if (context.FusionGraph.MutationType is null)
                {
                    targetType = context.FusionGraph.MutationType = new ObjectType(schema.MutationType.Name);
                    targetType.MergeDescriptionWith(schema.MutationType);
                    context.FusionGraph.Types.Add(targetType);
                }

                MergeRootFields(
                    context,
                    schema,
                    schema.MutationType,
                    targetType,
                    OperationType.Mutation,
                    skipOnMutation);
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
        ObjectType targetRootType,
        OperationType operationType,
        HashSet<string> skip)
    {
        if (!targetRootType.Name.EqualsOrdinal(sourceRootType.Name))
        {
            context.Log.Write(
                LogEntryHelper.RootTypeNameMismatch(
                    operationType,
                    targetRootType.Name,
                    sourceRootType.Name,
                    sourceSchema.Name));
            return;
        }

        foreach (var field in sourceRootType.Fields)
        {
            if (skip.Contains(field.Name))
            {
                continue;
            }

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

            var selectionSet = new SelectionSetNode(new[] { selection, });

            foreach (var arg in field.Arguments)
            {
                arguments.Add(new ArgumentNode(arg.Name, new VariableNode(arg.Name)));
                context.ApplyVariable(targetField, arg, sourceSchema.Name);
            }

            context.ApplyResolvers(targetField, selectionSet, sourceSchema.Name);
        }
    }
}

file static class MergeQueryTypeMiddlewareExtensions
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