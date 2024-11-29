using HotChocolate.Language;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.LogEntryHelper;
using static HotChocolate.Fusion.Composition.Properties.CompositionResources;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal class EntityFieldDependencyMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var entity in context.Entities)
        {
            var entityType = (ObjectTypeDefinition)context.FusionGraph.Types[entity.Name];
            context.ApplyDependencies(entityType, entity.Metadata);
        }

        if (!context.Log.HasErrors)
        {
            await next(context);
        }
    }
}

static file class MergeEntitiesMiddlewareExtensions
{
    public static void ApplyDependencies(
        this CompositionContext context,
        ObjectTypeDefinition entityType,
        EntityMetadata metadata)
    {
        var arguments = new Dictionary<string, ITypeNode>();
        var argumentRefLookup = new Dictionary<string, string>();

        foreach (var (fieldName, dependantFields) in metadata.DependantFields)
        {
            if (entityType.Fields.TryGetField(fieldName, out var field))
            {
                foreach (var dependency in dependantFields)
                {
                    arguments.Clear();
                    argumentRefLookup.Clear();

                    ResolveDependencies(
                        context,
                        entityType,
                        field,
                        dependency,
                        arguments,
                        argumentRefLookup);

                    if (!context.TryGetSubgraphMember<OutputFieldDefinition>(
                        dependency.SubgraphName,
                        new SchemaCoordinate(entityType.Name, field.Name),
                        out var subgraphField))
                    {
                        // This can only happen if there was an issue in the MergeEntityMiddleware.
                        throw new InvalidOperationException(CannotFindCorrelatingSubgraphField);
                    }

                    foreach (var argument in field.Arguments)
                    {
                        if (!arguments.ContainsKey(argument.Name) &&
                            subgraphField.Arguments.TryGetField(argument.Name, out var arg))
                        {
                            arguments.Add(arg.GetOriginalName(), arg.Type.ToTypeNode());
                            argumentRefLookup.Add(arg.GetOriginalName(), argument.Name);
                        }
                    }

                    field.Directives.Add(
                        CreateResolverDirective(
                            context,
                            dependency.SubgraphName,
                            CreateFieldResolver(subgraphField.GetOriginalName(), argumentRefLookup),
                            arguments));
                }
            }
        }
    }

    private static void ResolveDependencies(
        CompositionContext context,
        ObjectTypeDefinition entityType,
        OutputFieldDefinition entityField,
        FieldDependency dependency,
        Dictionary<string, ITypeNode> arguments,
        Dictionary<string, string> argumentRefLookup)
    {
        foreach (var (argumentName, memberRef) in dependency.Arguments)
        {
            context.ResetSupportedBy();

            if (!context.CanResolveDependency(entityType, memberRef.Requirement))
            {
                context.Log.Write(
                    FieldDependencyCannotBeResolved(
                        new SchemaCoordinate(
                            entityType.Name,
                            entityField.Name,
                            argumentName),
                        memberRef.Requirement,
                        context.GetSubgraphSchema(dependency.SubgraphName)));
                continue;
            }

            var argumentRef = entityType.CreateVariableName(memberRef.Requirement);
            argumentRefLookup.Add(argumentName, argumentRef);
            arguments.Add(argumentRef, memberRef.Argument.Type.ToTypeNode());

            foreach (var subgraph in context.SupportedBy)
            {
                entityField.Directives.Add(
                    context.FusionTypes.CreateVariableDirective(
                        subgraph,
                        argumentRef,
                        memberRef.Requirement));
            }
        }
    }

    private static SelectionSetNode CreateFieldResolver(
        string fieldName,
        Dictionary<string, string> argumentMap)
    {
        var arguments = new List<ArgumentNode>();

        foreach (var (argumentName, variableName) in argumentMap)
        {
            arguments.Add(new ArgumentNode(argumentName, new VariableNode(variableName)));
        }

        var field = new FieldNode(
            null,
            new NameNode(fieldName),
            null,
            Array.Empty<DirectiveNode>(),
            arguments,
            null);

        return new SelectionSetNode(new[] { field, });
    }

    private static Directive CreateResolverDirective(
        CompositionContext context,
        string subgraphName,
        SelectionSetNode select,
        Dictionary<string, ITypeNode>? arguments = null,
        EntityResolverKind kind = EntityResolverKind.Single)
        => context.FusionTypes.CreateResolverDirective(
            subgraphName,
            select,
            arguments,
            kind);
}
