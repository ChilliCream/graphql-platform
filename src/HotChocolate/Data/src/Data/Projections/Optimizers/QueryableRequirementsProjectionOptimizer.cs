using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Requirements;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Projections.Optimizers;

public sealed class QueryableRequirementsProjectionOptimizer : IProjectionOptimizer
{
    private const string AliasPrefix = "__projection_requirements_";

    public bool CanHandle(Selection field)
        => (field.Field.Flags & CoreFieldFlags.WithRequirements) == CoreFieldFlags.WithRequirements;

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        if (!context.Schema.Features.TryGet(out FieldRequirementsMetadata? metadata))
        {
            return selection;
        }

        var requirements = CollectRequirements(context, metadata);

        foreach (var requirement in requirements)
        {
            if (!TryGetField(context.TypeContext, requirement.Property, out var field)
                || field.Arguments.Count > 0)
            {
                continue;
            }

            var responseName = AliasPrefix + field.Name;
            var fieldNode = CreateFieldNode(field, requirement, responseName);
            UpsertInternalSelection(context, responseName, field, fieldNode);
        }

        return selection;
    }

    private static IReadOnlyList<PropertyNode> CollectRequirements(
        SelectionSetOptimizerContext context,
        FieldRequirementsMetadata metadata)
    {
        var root = new TypeNode(context.TypeContext.RuntimeType);

        foreach (var selection in context.Selections)
        {
            if (selection.IsInternal)
            {
                continue;
            }

            if ((selection.Field.Flags & CoreFieldFlags.WithRequirements) != CoreFieldFlags.WithRequirements)
            {
                continue;
            }

            if (metadata.GetRequirements(selection.Field) is not { } requirements)
            {
                continue;
            }

            foreach (var node in requirements.Nodes)
            {
                root.TryAddNode(node.Clone());
            }
        }

        return root.Nodes;
    }

    private static void UpsertInternalSelection(
        SelectionSetOptimizerContext context,
        string responseName,
        ObjectField field,
        FieldNode fieldNode)
    {
        var resolverPipeline = context.CompileResolverPipeline(field, fieldNode);

        if (context.TryGetSelection(responseName, out var existingSelection))
        {
            if (existingSelection.IsInternal && existingSelection.Field == field)
            {
                context.ReplaceSelection(
                    new Selection(
                        existingSelection.Id,
                        responseName,
                        field,
                        [new FieldSelectionNode(fieldNode, 0)],
                        [],
                        isInternal: true,
                        resolverPipeline: resolverPipeline));
            }

            return;
        }

        context.AddSelection(
            new Selection(
                context.NewSelectionId(),
                responseName,
                field,
                [new FieldSelectionNode(fieldNode, 0)],
                [],
                isInternal: true,
                resolverPipeline: resolverPipeline));
    }

    private static FieldNode CreateFieldNode(
        ObjectField field,
        PropertyNode requirement,
        string responseName)
    {
        var selectionSet = CreateSelectionSet(requirement.Nodes, field.Type.NamedType());

        return new FieldNode(
            null,
            new NameNode(field.Name),
            new NameNode(responseName),
            [],
            [],
            selectionSet);
    }

    private static SelectionSetNode? CreateSelectionSet(
        IReadOnlyList<TypeNode> requirements,
        ITypeDefinition namedType)
    {
        if (requirements.Count == 0)
        {
            return null;
        }

        var mergedNode = new TypeNode(requirements[0].Type);

        foreach (var requirement in requirements)
        {
            foreach (var node in requirement.Nodes)
            {
                mergedNode.TryAddNode(node.Clone());
            }
        }

        var selections = new List<ISelectionNode>();

        foreach (var requirement in mergedNode.Nodes)
        {
            if (!TryGetField(namedType, requirement.Property, out var field))
            {
                continue;
            }

            selections.Add(
                new FieldNode(
                    null,
                    new NameNode(field.Name),
                    null,
                    [],
                    [],
                    CreateSelectionSet(requirement.Nodes, field.Type.NamedType())));
        }

        return selections.Count == 0 ? null : new SelectionSetNode(selections);
    }

    private static bool TryGetField(
        ObjectType type,
        PropertyInfo property,
        out ObjectField field)
    {
        foreach (var candidate in type.Fields)
        {
            if (IsMatchingField(candidate, property))
            {
                field = candidate;
                return true;
            }
        }

        field = default!;
        return false;
    }

    private static bool TryGetField(
        ITypeDefinition namedType,
        PropertyInfo property,
        out IOutputFieldDefinition field)
    {
        switch (namedType)
        {
            case ObjectType objectType:
                foreach (var candidate in objectType.Fields)
                {
                    if (IsMatchingField(candidate, property))
                    {
                        field = candidate;
                        return true;
                    }
                }
                break;

            case InterfaceType interfaceType:
                foreach (var candidate in interfaceType.Fields)
                {
                    if (NameMatches(candidate.Name, property))
                    {
                        field = candidate;
                        return true;
                    }
                }
                break;
        }

        field = default!;
        return false;
    }

    private static bool IsMatchingField(ObjectField field, PropertyInfo property)
    {
        if (field.Member is PropertyInfo member)
        {
            return AreSameProperty(member, property);
        }

        return NameMatches(field.Name, property);
    }

    private static bool NameMatches(string fieldName, PropertyInfo property)
        => fieldName.Equals(property.Name, StringComparison.Ordinal)
            || fieldName.Equals(ToCamelCase(property.Name), StringComparison.Ordinal);

    private static bool AreSameProperty(PropertyInfo left, PropertyInfo right)
        => ReferenceEquals(left, right)
            || left.Name.Equals(right.Name, StringComparison.Ordinal)
                && left.DeclaringType == right.DeclaringType
            || left.MetadataToken == right.MetadataToken
                && left.Module.Equals(right.Module);

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value) || !char.IsUpper(value[0]))
        {
            return value;
        }

        if (value.Length == 1)
        {
            return char.ToLowerInvariant(value[0]).ToString();
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    public static QueryableRequirementsProjectionOptimizer Create(ProjectionProviderContext context) => new();
}
