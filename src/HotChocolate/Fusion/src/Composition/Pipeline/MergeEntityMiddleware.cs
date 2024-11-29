using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Composition.LogEntryHelper;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal class MergeEntityMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var entity in context.Entities)
        {
            var entityType = (ObjectTypeDefinition)context.FusionGraph.Types[entity.Name];

            foreach (var part in entity.Parts)
            {
                context.Merge(part, entityType);
            }

            context.ApplyResolvers(entityType, entity);
        }

        if (!context.Log.HasErrors)
        {
            await next(context);
        }
    }
}

file static class MergeEntitiesMiddlewareExtensions
{
    public static void Merge(this CompositionContext context, EntityPart source, ObjectTypeDefinition target)
    {
        context.TryApplySource(source.Type, source.Schema, target);

        target.MergeDescriptionWith(source.Type);

        target.MergeDirectivesWith(source.Type, context);

        foreach (var interfaceType in source.Type.Implements)
        {
            if (!target.Implements.Any(t => t.Name.EqualsOrdinal(interfaceType.Name)))
            {
                target.Implements.Add((InterfaceTypeDefinition)context.FusionGraph.Types[interfaceType.Name]);
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
        ObjectTypeDefinition entityType,
        EntityGroup entity)
    {
        var variables = new HashSet<string>();

        foreach (var resolver in entity.Metadata.EntityResolvers)
        {
            foreach (var variable in resolver.Variables)
            {
                context.ResetSupportedBy();

                if (variables.Add(variable.Key))
                {
                    if (!context.CanResolveDependency(entityType, variable.Value.Field))
                    {
                        FieldDependencyCannotBeResolved(
                            new SchemaCoordinate(
                                entityType.Name,
                                variable.Value.Field.Name.Value),
                            variable.Value.Field,
                            context.GetSubgraphSchema(resolver.SubgraphName));
                    }

                    foreach (var subgraph in context.SupportedBy)
                    {
                        entityType.Directives.Add(
                            context.FusionTypes.CreateVariableDirective(
                                subgraph,
                                variable.Key,
                                variable.Value.Field));
                    }
                }
            }
        }

        foreach (var resolver in entity.Metadata.EntityResolvers)
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
        string variableName,
        string subgraphName)
        => context.FusionTypes.CreateVariableDirective(
            subgraphName,
            variableName,
            variableName);
}
