using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal class MergeEntityMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var entity in context.Entities)
        {
            var entityType = (ObjectType)context.FusionGraph.Types[entity.Name];

            foreach (var part in entity.Parts)
            {
                context.Merge(part, entityType);
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
    public static void Merge(this CompositionContext context, EntityPart source, ObjectType target)
    {
        context.TryApplySource(source.Type, source.Schema, target);

        if (string.IsNullOrEmpty(target.Description))
        {
            target.Description = source.Type.Description;
        }

        foreach (var interfaceType in source.Type.Implements)
        {
            if (!target.Implements.Any(t => t.Name.EqualsOrdinal(interfaceType.Name)))
            {
                target.Implements.Add((InterfaceType)context.FusionGraph.Types[interfaceType.Name]);
            }
        }

        foreach (var sourceField in source.Type.Fields)
        {
            if (target.Fields.TryGetField(sourceField.Name, out var targetField))
            {
                context.MergeField(sourceField, targetField, source.Type.Name);
            }
            else
            {
                targetField = context.CreateField(sourceField, context.FusionGraph);
                target.Fields.Add(targetField);
            }

            context.ApplySource(sourceField, source.Schema, targetField);


            foreach (var argument in targetField.Arguments)
            {
                targetField.Directives.Add(
                    CreateVariableDirective(
                        context,
                        argument.Name,
                        argument.Type,
                        source.Schema.Name));
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
                        resolver.Subgraph));
            }
        }
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
                argument.Type,
                subgraphName));
    }

    private static Directive CreateResolverDirective(
        CompositionContext context,
        EntityResolver resolver)
        => context.FusionTypes.CreateResolverDirective(
            resolver.Subgraph,
            resolver.SelectionSet);

    private static Directive CreateVariableDirective(
        CompositionContext context,
        KeyValuePair<string, VariableDefinition> variable,
        string schemaName)
        => context.FusionTypes.CreateVariableDirective(
            schemaName,
            variable.Key,
            variable.Value.Field,
            variable.Value.Definition.Type);

    private static Directive CreateVariableDirective(
        CompositionContext context,
        string variableName,
        IType argumentType,
        string subgraphName)
        => context.FusionTypes.CreateVariableDirective(
            subgraphName,
            variableName,
            variableName,
            argumentType.ToTypeNode());
}
