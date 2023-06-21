using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Composition.LogEntryHelper;
using static HotChocolate.Fusion.Composition.Properties.CompositionResources;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal class EntityFieldDependencyMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var entity in context.Entities)
        {
            var entityType = (ObjectType)context.FusionGraph.Types[entity.Name];
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
        ObjectType entityType,
        EntityMetadata metadata)
    {
        var supportedBy = new HashSet<string>();
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
                        supportedBy,
                        arguments,
                        argumentRefLookup);

                    if (!context.TryGetSubgraphMember<OutputField>(
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
        ObjectType entityType,
        OutputField entityField,
        FieldDependency dependency,
        HashSet<string> supportedBy,
        Dictionary<string, ITypeNode> arguments,
        Dictionary<string, string> argumentRefLookup)
    {
        foreach (var (argumentName, memberRef) in dependency.Arguments)
        {
            foreach (var subgraph in context.Subgraphs)
            {
                supportedBy.Add(subgraph.Name);
            }

            if (!CanResolve(
                context,
                entityType,
                memberRef.Requirement,
                supportedBy))
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

            foreach (var subgraph in supportedBy)
            {
                entityField.Directives.Add(
                    context.FusionTypes.CreateVariableDirective(
                        subgraph,
                        argumentRef,
                        memberRef.Requirement));
            }
        }
    }

    private static bool CanResolve(
        CompositionContext context,
        ComplexType complexType,
        FieldNode fieldRef,
        ISet<string> supportedBy)
    {
        // not supported yet.
        if (fieldRef.Arguments.Count > 0)
        {
            return false;
        }

        if (!complexType.Fields.TryGetField(fieldRef.Name.Value, out var fieldDef))
        {
            return false;
        }

        if (fieldRef.SelectionSet is not null)
        {
            if (fieldDef.Type.NamedType() is not ComplexType namedType)
            {
                return false;
            }

            return CanResolveChildren(context, namedType, fieldRef.SelectionSet, supportedBy);
        }

        supportedBy.IntersectWith(
            fieldDef.Directives
                .Where(t => t.Name.EqualsOrdinal(context.FusionTypes.Source.Name))
                .Select(t => ((StringValueNode)t.Arguments[SubgraphArg]).Value));

        return supportedBy.Count > 0;
    }

    private static bool CanResolveChildren(
        CompositionContext context,
        ComplexType complexType,
        SelectionSetNode selectionSet,
        ISet<string> supportedBy)
    {
        if (selectionSet.Selections.Count != 1)
        {
            return false;
        }

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode fieldNode)
            {
                return CanResolve(context, complexType, fieldNode, supportedBy);
            }
            else if (selection is InlineFragmentNode inlineFragment)
            {
                if (inlineFragment.TypeCondition is null ||
                    !context.FusionGraph.Types.TryGetType<ComplexType>(
                        inlineFragment.TypeCondition.Name.Value,
                        out var fragmentType))
                {
                    return false;
                }

                return CanResolveChildren(
                    context,
                    fragmentType,
                    inlineFragment.SelectionSet,
                    supportedBy);
            }
            else
            {
                return false;
            }
        }

        return true;
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
            null,
            Array.Empty<DirectiveNode>(),
            arguments,
            null);

        return new SelectionSetNode(new[] { field });
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
