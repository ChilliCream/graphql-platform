using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class MergeQueryAndMutationTypeMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var skipOnQuery = new HashSet<string>(StringComparer.Ordinal)
        {
            // Fusion can currently not handle the `nodes` field, so we're not exposing it
            // through the gateway schema, even though a subgraph might support it.
            "nodes"
        };
        var skipOnMutation = new HashSet<string>(StringComparer.Ordinal);

        if (!context.Features.IsNodeFieldSupported())
        {
            skipOnQuery.Add("node");
        }

        foreach (var schema in context.Subgraphs)
        {
            if (schema.QueryType is not null)
            {
                var targetType = context.FusionGraph.QueryType!;

                if (context.FusionGraph.QueryType is null)
                {
                    targetType = context.FusionGraph.QueryType = new ObjectTypeDefinition(schema.QueryType.Name);
                    targetType.MergeDescriptionWith(schema.QueryType);
                    targetType.MergeDirectivesWith(schema.QueryType, context);
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
                    targetType = context.FusionGraph.MutationType = new ObjectTypeDefinition(schema.MutationType.Name);
                    targetType.MergeDescriptionWith(schema.MutationType);
                    targetType.MergeDirectivesWith(schema.MutationType, context);
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
        SchemaDefinition sourceSchema,
        ObjectTypeDefinition sourceRootType,
        ObjectTypeDefinition targetRootType,
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

            if (field.ContainsInternalDirective())
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
                Array.Empty<DirectiveNode>(),
                arguments,
                null);

            var selectionSet = new SelectionSetNode([selection]);

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
        OutputFieldDefinition field,
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
        OutputFieldDefinition field,
        InputFieldDefinition argument,
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
