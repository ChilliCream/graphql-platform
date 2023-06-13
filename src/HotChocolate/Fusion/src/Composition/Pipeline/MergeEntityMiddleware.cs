using HotChocolate.Language;
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
                        source.Schema.Name));
            }
        }
    }

    public static void ApplyResolvers(
        this CompositionContext context,
        ObjectType entityType,
        EntityMetadata metadata)
    {
        var variables = new HashSet<(string, string)>();

        foreach (var resolver in metadata.EntityResolvers)
        {
            foreach (var variable in resolver.Variables)
            {
                if (variables.Add((variable.Key, resolver.SubgraphName)))
                {
                    entityType.Directives.Add(
                        CreateVariableDirective(
                            context,
                            variable,
                            resolver.SubgraphName));
                }
            }
        }

        foreach (var resolver in metadata.EntityResolvers)
        {
            Dictionary<string, ITypeNode>? arguments = null;

            foreach (var variable in resolver.Variables)
            {
                arguments ??= new Dictionary<string, ITypeNode>();
                arguments.Add(variable.Key, variable.Value.Definition.Type);
            }

            entityType.Directives.Add(
                CreateResolverDirective(
                    context,
                    resolver,
                    arguments,
                    resolver.Kind));
        }
    }

    private static Directive CreateResolverDirective(
        CompositionContext context,
        EntityResolver resolver,
        Dictionary<string, ITypeNode>? arguments = null,
        EntityResolverKind kind = EntityResolverKind.Single)
        => context.FusionTypes.CreateResolverDirective(
            resolver.SubgraphName,
            resolver.SelectionSet,
            arguments,
            kind);

    private static Directive CreateVariableDirective(
        CompositionContext context,
        KeyValuePair<string, VariableDefinition> variable,
        string schemaName)
        => context.FusionTypes.CreateVariableDirective(
            schemaName,
            variable.Key,
            variable.Value.Field);

    private static Directive CreateVariableDirective(
        CompositionContext context,
        string variableName,
        string subgraphName)
        => context.FusionTypes.CreateVariableDirective(
            subgraphName,
            variableName,
            variableName);
}
