using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;

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

                    foreach (var (argumentName, memberRef) in dependency.Arguments)
                    {
                        foreach (var subgraph in context.Subgraphs)
                        {
                            supportedBy.Add(subgraph.Name);
                        }

                        if (memberRef.Reference.IsCoordinate)
                        {
                            // TODO : ERROR
                            context.Log.Write(
                                new LogEntry(
                                    "A coordinate is not allowed when declaring requirements.",
                                    severity: LogSeverity.Error,
                                    coordinate: new SchemaCoordinate(
                                        entityType.Name,
                                        field.Name,
                                        argumentName)));
                            continue;
                        }

                        if (!CanResolve(
                            context,
                            entityType,
                            memberRef.Reference.Field,
                            supportedBy))
                        {
                            // TODO : ERROR
                            context.Log.Write(
                                new LogEntry(
                                    string.Format(
                                        "The field dependency `{0}` cannot be resolved.",
                                        memberRef.Reference.Field),
                                    severity: LogSeverity.Error,
                                    coordinate: new SchemaCoordinate(
                                        entityType.Name,
                                        field.Name,
                                        argumentName)));
                            continue;
                        }

                        var argumentRef = string.Format("_{0}_{1}", dependency.Id, argumentName);
                        argumentRefLookup.Add(argumentName, argumentRef);
                        arguments.Add(argumentName, memberRef.Argument.Type.ToTypeNode());

                        foreach (var subgraph in supportedBy)
                        {
                            field.Directives.Add(
                                context.FusionTypes.CreateVariableDirective(
                                    subgraph,
                                    argumentRef,
                                    memberRef.Reference.Field));
                        }
                    }

                    if (!context.TryGetSubgraphMember<OutputField>(
                        dependency.SubgraphName,
                        new SchemaCoordinate(entityType.Name, field.Name),
                        out var subgraphField))
                    {
                        throw new InvalidOperationException("TODO : ERROR");
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
