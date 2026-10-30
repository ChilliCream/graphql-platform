using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed partial record OperationPlan
{
    private readonly Dictionary<int, DeliveryPath>? _deliveryPaths;

    internal DeliveryPath GetDeliveryPath(int deliveryGroupId)
    {
        if (_deliveryPaths is not null
            && _deliveryPaths.TryGetValue(deliveryGroupId, out var deliveryPath))
        {
            return deliveryPath;
        }

        throw ThrowHelper.DeliveryGroupNotFound(deliveryGroupId);
    }

    private static Dictionary<int, DeliveryPath>? CreateDeliveryPaths(
        Operation operation,
        ImmutableArray<DeliveryGroup> deliveryGroups,
        ImmutableArray<IncrementalPlan> incrementalPlans)
    {
        if (deliveryGroups.IsDefaultOrEmpty)
        {
            return null;
        }

        if (incrementalPlans.IsDefault)
        {
            incrementalPlans = [];
        }

        var deliveryPaths = new Dictionary<int, DeliveryPath>(deliveryGroups.Length);

        foreach (var incrementalPlan in incrementalPlans)
        {
            foreach (var deliveryGroup in incrementalPlan.DeliveryGroups)
            {
                if (!deliveryPaths.ContainsKey(deliveryGroup.Id))
                {
                    deliveryPaths.Add(
                        deliveryGroup.Id,
                        CreateDeliveryPath(
                            incrementalPlan.Operation,
                            deliveryGroup.Path ?? SelectionPath.Root));
                }
            }
        }

        // A group that no plan delivers with is reached through the plan of a nested group.
        foreach (var incrementalPlan in incrementalPlans)
        {
            foreach (var deliveryGroup in incrementalPlan.DeliveryGroups)
            {
                for (var ancestor = deliveryGroup.Parent; ancestor is not null; ancestor = ancestor.Parent)
                {
                    if (!deliveryPaths.ContainsKey(ancestor.Id))
                    {
                        deliveryPaths.Add(
                            ancestor.Id,
                            CreateDeliveryPath(
                                incrementalPlan.Operation,
                                ancestor.Path ?? SelectionPath.Root));
                    }
                }
            }
        }

        foreach (var deliveryGroup in deliveryGroups)
        {
            if (!deliveryPaths.ContainsKey(deliveryGroup.Id))
            {
                deliveryPaths.Add(
                    deliveryGroup.Id,
                    CreateDeliveryPath(
                        operation,
                        deliveryGroup.Path ?? SelectionPath.Root));
            }
        }

        return deliveryPaths;
    }

    private static DeliveryPath CreateDeliveryPath(
        Operation operation,
        SelectionPath selectionPath)
    {
        var path = Path.Root;
        var pendingFieldCount = 0;
        var currentSelectionSet = operation.Definition.SelectionSet;
        IOutputTypeDefinition currentType = operation.RootType;

        for (var i = 0; i < selectionPath.Length; i++)
        {
            var segment = selectionPath[i];

            if (segment.Kind is SelectionPathSegmentKind.Root)
            {
                continue;
            }

            if (segment.Kind is SelectionPathSegmentKind.InlineFragment)
            {
                if (!TryFindInlineFragment(currentSelectionSet, segment.Name, out var inlineFragment)
                    || !operation.Schema.Types.TryGetType<IOutputTypeDefinition>(segment.Name, out var fragmentType))
                {
                    throw ThrowHelper.DeliveryPathSegmentNotFound(segment.Name);
                }

                currentSelectionSet = inlineFragment.SelectionSet;
                currentType = fragmentType;
                continue;
            }

            if (!TryFindField(
                operation.Schema,
                currentSelectionSet,
                currentType,
                segment.Name,
                out var fieldNode,
                out var field))
            {
                throw ThrowHelper.DeliveryPathSegmentNotFound(segment.Name);
            }

            path = path.Append(fieldNode.Alias?.Value ?? fieldNode.Name.Value);
            pendingFieldCount++;

            if (field.Type.IsListType())
            {
                break;
            }

            currentSelectionSet = fieldNode.SelectionSet
                ?? throw ThrowHelper.DeliveryPathSelectionSetMissing(segment.Name);
            currentType = field.Type.NamedType<IOutputTypeDefinition>();
        }

        return new DeliveryPath(path, pendingFieldCount);
    }

    private static bool TryFindField(
        ISchemaDefinition schema,
        SelectionSetNode selectionSet,
        IOutputTypeDefinition typeContext,
        string responseName,
        out FieldNode fieldNode,
        out IOutputFieldDefinition field)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode candidate
                && (candidate.Alias?.Value ?? candidate.Name.Value) == responseName
                && typeContext is IComplexTypeDefinition complexType
                && complexType.Fields.TryGetField(candidate.Name.Value, out var resolvedField)
                && resolvedField is not null)
            {
                fieldNode = candidate;
                field = resolvedField;
                return true;
            }

            if (selection is InlineFragmentNode inlineFragment)
            {
                IOutputTypeDefinition fragmentType;
                if (inlineFragment.TypeCondition?.Name.Value is not { } typeName)
                {
                    fragmentType = typeContext;
                }
                else if (schema.Types.TryGetType<IOutputTypeDefinition>(
                    typeName,
                    out var resolvedType)
                    && resolvedType is not null)
                {
                    fragmentType = resolvedType;
                }
                else
                {
                    continue;
                }

                if (TryFindField(
                    schema,
                    inlineFragment.SelectionSet,
                    fragmentType,
                    responseName,
                    out fieldNode,
                    out field))
                {
                    return true;
                }
            }
        }

        fieldNode = null!;
        field = null!;
        return false;
    }

    private static bool TryFindInlineFragment(
        SelectionSetNode selectionSet,
        string typeName,
        out InlineFragmentNode inlineFragment)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not InlineFragmentNode candidate)
            {
                continue;
            }

            if (candidate.TypeCondition?.Name.Value == typeName)
            {
                inlineFragment = candidate;
                return true;
            }

            if (TryFindInlineFragment(candidate.SelectionSet, typeName, out inlineFragment))
            {
                return true;
            }
        }

        inlineFragment = null!;
        return false;
    }

    internal readonly record struct DeliveryPath(Path PendingPath, int PendingFieldCount);
}
