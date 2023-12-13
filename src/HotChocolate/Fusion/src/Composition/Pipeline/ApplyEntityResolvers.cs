using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class ApplyEntityResolvers : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var fieldContext = FieldContext.Create(context);
        var variables = new HashSet<VariableDirective>();
        var resolvers = new HashSet<ResolverDirective>();
        var argumentMap = new Dictionary<string, string>();

        foreach (var group in context.EntityResolverInfos.GroupBy(t => t.EntityName))
        {
            if (!context.FusionGraph.Types.TryGetType<ObjectType>(group.Key, out var type))
            {
                // TODO: log error
                continue;
            }

            foreach (var entityResolver in group)
            {
                foreach (var sourceArgument in entityResolver.SourceArguments)
                {
                    foreach (var variable in CreateVariables(fieldContext, type, sourceArgument))
                    {
                        argumentMap[sourceArgument.Argument.Name] = variable.Name;
                        variables.Add(variable);
                    }
                }

                var originalEntity = entityResolver.SourceField.Schema.Types[entityResolver.EntityName];

                var resolver = CreateResolver(
                    entityResolver.SourceField.Schema.Name,
                    type,
                    (ObjectType)originalEntity,
                    entityResolver.Kind,
                    entityResolver.SourceField,
                    entityResolver.SourceArguments,
                    argumentMap);
                
                resolvers.Add(resolver);
            }

            foreach (var variable in variables.OrderBy(t => t.Name).ThenBy(t => t.Subgraph))
            {
                type.Directives.Add(variable.ToDirective(context.FusionTypes));
            }
            
            foreach (var resolver in resolvers.OrderBy(t => t.Subgraph))
            {
                type.Directives.Add(resolver.ToDirective(context.FusionTypes));
                SourceDirective.RemoveFrom(type, context.FusionTypes, resolver.Subgraph);
            }
        }
        
        if (!context.Log.HasErrors)
        {
            await next(context);
        }
    }

    private static ResolverDirective CreateResolver(
        string subgraphName,
        ObjectType entity,
        ObjectType originalEntity,
        ResolverKind resolverKind,
        EntitySourceField entityResolver,
        IReadOnlyList<EntitySourceArgument> sourceArguments,
        Dictionary<string, string> argumentMap)
    {
        var operation = new OperationDefinitionNode(
            null,
            OperationType.Query,
            CreateVariables(sourceArguments, argumentMap),
            Array.Empty<DirectiveNode>(),
            CreateSelectionSet(entity, originalEntity, entityResolver, sourceArguments, argumentMap));
        
        return new ResolverDirective(operation, resolverKind, subgraphName);
    }

    private static IReadOnlyList<VariableDefinitionNode> CreateVariables(
        IReadOnlyList<EntitySourceArgument> sourceArguments,
        Dictionary<string, string> argumentMap)
    {
        if (sourceArguments.Count == 0)
        {
            return Array.Empty<VariableDefinitionNode>();
        }
        
        var variables = new VariableDefinitionNode[sourceArguments.Count];
        
        for (var i = 0; i < sourceArguments.Count; i++)
        {
            var argument = sourceArguments[i].Argument;
            var type = argument.Type.ToTypeNode(argument.Type.NamedType().GetOriginalName());
            var variable = new VariableNode(argumentMap[argument.Name]);
            variables[i] = new VariableDefinitionNode(variable, type, null, Array.Empty<DirectiveNode>());
        }

        return variables;
    }
    
    private static SelectionSetNode CreateSelectionSet(
        ObjectType entity,
        ObjectType originalEntity,
        EntitySourceField entityResolver,
        IReadOnlyList<EntitySourceArgument> sourceArguments,
        Dictionary<string, string> argumentMap)
    {
        SelectionSetNode? selectionSet = null;
        
        var resolverReturnType = entityResolver.Field.Type.NamedType();
        if (resolverReturnType.Kind is TypeKind.Interface or TypeKind.Union &&  
            resolverReturnType.IsAssignableFrom(originalEntity))
        {
            selectionSet = new SelectionSetNode(
                new[]
                {
                    new InlineFragmentNode(
                        null, 
                        new NamedTypeNode(originalEntity.Name), 
                        Array.Empty<DirectiveNode>(),
                        new SelectionSetNode(
                            new[]
                            {
                                new FragmentSpreadNode(
                                    null, 
                                    new NameNode(entity.Name), 
                                    Array.Empty<DirectiveNode>())
                            }))
                });
        }
        
        var selection = new FieldNode(
            null,
            new NameNode(entityResolver.Field.GetOriginalName()),
            null,
            null,
            Array.Empty<DirectiveNode>(),
            CreateArguments(sourceArguments, argumentMap),
            selectionSet);

        return new SelectionSetNode(new[] { selection });
    }

    private static IReadOnlyList<ArgumentNode> CreateArguments(
        IReadOnlyList<EntitySourceArgument> sourceArguments,
        Dictionary<string, string> argumentMap)
    {
        if (sourceArguments.Count == 0)
        {
            return Array.Empty<ArgumentNode>();
        }
        
        var argumentNodes = new ArgumentNode[sourceArguments.Count];

        for (var i = 0; i < sourceArguments.Count; i++)
        {
            var argument = sourceArguments[i].Argument;
            argumentNodes[i] = new ArgumentNode(argument.Name, new VariableNode(argumentMap[argument.Name]));
        }

        return argumentNodes;
    }

    private static IEnumerable<VariableDirective> CreateVariables(
        FieldContext context,
        ObjectType typeContext,
        EntitySourceArgument sourceArgument)
    {
        context.Clear();

        DetermineSubgraphs(context, typeContext, sourceArgument.Directive.Field!);

        foreach (var subgraph in context.Subgraphs)
        {
            yield return new VariableDirective(
                typeContext.CreateVariableName(sourceArgument.Directive),
                sourceArgument.Directive.Field!,
                subgraph);
        }
    }

    private static void DetermineSubgraphs(
        FieldContext context,
        INamedType typeContext,
        ISelectionNode selection)
    {
        // note: we need to refine this... at the moment input objects are problematic as we do not have
        // a marker to indicate that we are in an input object.

        // note: we need to create errors when something cannot be handled.

        context.FieldSubgraphs.Clear();

        switch (selection)
        {
            case FieldNode fieldNode:
                if (TryGetField(typeContext, fieldNode.Name.Value, out var field))
                {
                    foreach (var source in SourceDirective.GetAllFrom(field, context.FusionTypes))
                    {
                        context.FieldSubgraphs.Add(source.Subgraph);
                    }

                    context.Subgraphs.IntersectWith(context.FieldSubgraphs);

                    if (fieldNode.SelectionSet is { } selectionSet)
                    {
                        foreach (var child in selectionSet.Selections)
                        {
                            DetermineSubgraphs(context, typeContext, child);
                        }
                    }
                }
                break;

            case InlineFragmentNode inlineFragmentNode:
                if (IsAssignable(context, inlineFragmentNode.TypeCondition, typeContext))
                {
                    foreach (var child in inlineFragmentNode.SelectionSet.Selections)
                    {
                        DetermineSubgraphs(context, typeContext, child);
                    }
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(selection));
        }
    }

    private static bool TryGetField(
        INamedType type,
        string fieldName,
        [NotNullWhen(true)] out OutputField? field)
    {
        switch (type)
        {
            case InterfaceType interfaceType:
                if (interfaceType.Fields.TryGetField(fieldName, out field))
                {
                    return true;
                }
                break;

            case ObjectType objectType:
                if (objectType.Fields.TryGetField(fieldName, out field))
                {
                    return true;
                }
                break;

            default:
                field = null;
                return false;
        }

        field = null;
        return false;
    }

    private static bool IsAssignable(FieldContext context, NamedTypeNode? typeCondition, INamedType type)
    {
        if (typeCondition is null)
        {
            return true;
        }

        // todo: we should have an error here
        return context.FusionGraph.Types.TryGetType(typeCondition.Name.Value, out var typeConditionType) &&
            typeConditionType.IsAssignableFrom(type);
    }

    private readonly struct FieldContext(
        CompositionContext context,
        HashSet<string> subgraphs,
        HashSet<string> fieldSubgraphs)
    {
        public FusionTypes FusionTypes => context.FusionTypes;

        public Schema FusionGraph => context.FusionGraph;

        public HashSet<string> Subgraphs { get; } = subgraphs;

        public HashSet<string> FieldSubgraphs { get; } = fieldSubgraphs;

        public void Clear()
        {
            Subgraphs.Clear();
            FieldSubgraphs.Clear();

            foreach (var subgraph in context.Subgraphs)
            {
                Subgraphs.Add(subgraph.Name);
            }
        }

        public static FieldContext Create(CompositionContext context)
            => new(context, new HashSet<string>(), new HashSet<string>());
    }
}