using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class MergeSubscriptionTypeMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var schema in context.Subgraphs)
        {
            if (schema.SubscriptionType is null)
            {
                continue;
            }

            var subscriptionType = context.FusionGraph.SubscriptionType!;

            if (context.FusionGraph.SubscriptionType is null)
            {
                subscriptionType = context.FusionGraph.SubscriptionType = new ObjectTypeDefinition(schema.SubscriptionType.Name);
                subscriptionType.MergeDescriptionWith(schema.SubscriptionType);
                subscriptionType.MergeDirectivesWith(schema.SubscriptionType, context);
                context.FusionGraph.Types.Add(subscriptionType);
            }

            if (!subscriptionType.Name.EqualsOrdinal(schema.SubscriptionType.Name))
            {
                context.Log.Write(
                    LogEntryHelper.RootTypeNameMismatch(
                        OperationType.Subscription,
                        schema.SubscriptionType.Name,
                        subscriptionType.Name,
                        schema.Name));
                return;
            }

            foreach (var field in schema.SubscriptionType.Fields)
            {
                if (subscriptionType.Fields.TryGetField(field.Name, out var targetField))
                {
                    context.MergeField(field, targetField, subscriptionType.Name);
                }
                else
                {
                    targetField = context.CreateField(field, context.FusionGraph);
                    subscriptionType.Fields.Add(targetField);
                }

                var arguments = new List<ArgumentNode>();

                var selection = new FieldNode(
                    null,
                    new NameNode(field.GetOriginalName()),
                    null,
                    Array.Empty<DirectiveNode>(),
                    arguments,
                    null);

                var selectionSet = new SelectionSetNode(new[] { selection, });

                foreach (var arg in field.Arguments)
                {
                    arguments.Add(new ArgumentNode(arg.Name, new VariableNode(arg.Name)));
                    context.ApplyVariable(targetField, arg, schema.Name);
                }

                context.ApplyResolvers(targetField, selectionSet, schema.Name);
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}

static file class MergeSubscriptionTypeMiddlewareExtensions
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
            arguments,
            EntityResolverKind.Subscribe);

    private static Directive CreateVariableDirective(
        CompositionContext context,
        string variableName,
        string subgraphName)
        => context.FusionTypes.CreateVariableDirective(
            subgraphName,
            variableName,
            variableName);
}
