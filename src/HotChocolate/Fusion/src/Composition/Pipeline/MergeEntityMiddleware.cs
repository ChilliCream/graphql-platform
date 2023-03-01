using HotChocolate.Fusion.Composition.Extensions;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition;

public class MergeEntityMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var entity in context.Entities)
        {
            var entityType = (ObjectType)context.FusionGraph.Types[entity.Name];

            foreach (var part in entity.Parts)
            {
                context.Merge(part.Type, entityType);
            }

            context.ApplyResolvers(entityType, entity.Metadata);
        }

        if (!context.Log.HasErrors)
        {
            await next(context);
        }
    }
}

static file class MergeEntitiesMiddlewareExtensions
{
    public static void Merge(this CompositionContext context, ObjectType source, ObjectType target)
    {
        if (string.IsNullOrEmpty(target.Description))
        {
            source.Description = target.Description;
        }

        foreach (var interfaceType in source.Implements)
        {
            if (!target.Implements.Any(t => t.Name.EqualsOrdinal(interfaceType.Name)))
            {
                target.Implements.Add((InterfaceType)context.FusionGraph.Types[interfaceType.Name]);
            }
        }

        foreach (var sourceField in source.Fields)
        {
            if (target.Fields.TryGetField(sourceField.Name, out var targetField))
            {
                context.MergeField(sourceField, targetField);
            }
            else
            {
                target.Fields.Add(context.CreateField(sourceField));
            }
        }
    }

    public static void ApplyResolvers(
        this CompositionContext context,
        ObjectType entityType,
        EntityMetadata metadata)
    {
        foreach (var resolver in metadata.EntityResolvers)
        {
            entityType.Directives.Add(
                CreateResolverDirective(
                    context,
                    resolver));

            foreach (var variable in resolver.Variables)
            {
                entityType.Directives.Add(
                    CreateVariableDirective(
                        context,
                        variable,
                        resolver.SchemaName));
            }
        }
    }

    private static Directive CreateResolverDirective(
        CompositionContext context,
        EntityResolver resolver)
        => new Directive(
            context.FusionTypes.Resolver,
            new Argument(DirectiveArguments.Select, resolver.SelectionSet.ToString(false)),
            new Argument(DirectiveArguments.From, resolver.SchemaName));

    private static Directive CreateVariableDirective(
        CompositionContext context,
        KeyValuePair<string, VariableDefinition> variable,
        string schemaName)
        => new Directive(
            context.FusionTypes.Variable,
            new Argument(DirectiveArguments.Name, variable.Key),
            new Argument(DirectiveArguments.Select, variable.Value.Field.ToString(false)),
            new Argument(DirectiveArguments.From, schemaName),
            new Argument(DirectiveArguments.As, variable.Value.Definition.Type.ToString(false)));
}
