using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ErrorHelper;

namespace HotChocolate.Configuration.Validation;

internal sealed class ExecutionGroupValidationRule : ISchemaValidationRule
{
    public void Validate(
        IDescriptorContext context,
        ISchemaDefinition schema,
        ICollection<ISchemaError> errors)
    {
        foreach (var objectType in schema.Types.OfType<ObjectType>())
        {
            foreach (var field in objectType.Fields.AsSpan())
            {
                if (field.ExecutionGroup is { } group)
                {
                    ValidateGroup(schema, objectType, field, group, errors);
                }
            }
        }
    }

    private static void ValidateGroup(
        ISchemaDefinition schema,
        ObjectType type,
        ObjectField field,
        ExecutionGroup group,
        ICollection<ISchemaError> errors)
    {
        if (group.Entries.IsEmpty)
        {
            errors.Add(ExecutionGroup_NoEntries(type, field));
            return;
        }

        var paths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in group.Entries)
        {
            if (!paths.Add(entry.Path))
            {
                errors.Add(ExecutionGroup_DuplicateEntry(type, field, entry));
                continue;
            }

            if (ValidateEntry(schema, type, field, entry) is { } error)
            {
                errors.Add(error);
            }
        }

        foreach (var entry in group.Entries)
        {
            foreach (var other in group.Entries)
            {
                if (IsNestedBelow(entry, other))
                {
                    errors.Add(ExecutionGroup_NestedEntry(type, field, entry, other));
                    break;
                }
            }
        }
    }

    private static bool IsNestedBelow(ExecutionGroupEntry entry, ExecutionGroupEntry other)
    {
        if (other.Segments.Length >= entry.Segments.Length)
        {
            return false;
        }

        for (var i = 0; i < other.Segments.Length; i++)
        {
            if (!string.Equals(entry.Segments[i], other.Segments[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static ISchemaError? ValidateEntry(
        ISchemaDefinition schema,
        ObjectType type,
        ObjectField root,
        ExecutionGroupEntry entry)
    {
        foreach (var name in entry.Segments)
        {
            if (!name.IsValidGraphQLName())
            {
                return ExecutionGroup_InvalidFieldName(type, root, entry, name);
            }
        }

        switch (root.Type.NamedType())
        {
            case ObjectType objectType:
                return ValidatePath(type, root, objectType, entry);

            case InterfaceType interfaceType:
                foreach (var possibleType in schema.GetPossibleTypes(interfaceType))
                {
                    if (ValidatePath(type, root, possibleType, entry) is { } error)
                    {
                        return error;
                    }
                }

                return null;

            default:
                return ExecutionGroup_RootTypeNotComposite(type, root, entry);
        }
    }

    private static ISchemaError? ValidatePath(
        ObjectType type,
        ObjectField root,
        IComplexTypeDefinition currentType,
        ExecutionGroupEntry entry)
    {
        for (var i = 0; i < entry.Segments.Length; i++)
        {
            var segment = entry.Segments[i];

            if (!currentType.Fields.TryGetField(segment, out var field))
            {
                return ExecutionGroup_FieldNotFound(type, root, entry, currentType.Name, segment);
            }

            if (field is ObjectField { BatchResolver: not null })
            {
                return ExecutionGroup_BatchResolverField(type, root, entry, currentType.Name, segment);
            }

            if (field is ObjectField { IsParallelExecutable: false })
            {
                return ExecutionGroup_NotParallelExecutableField(type, root, entry, currentType.Name, segment);
            }

            if (i == entry.Segments.Length - 1)
            {
                break;
            }

            if (field.Type.IsListType())
            {
                return ExecutionGroup_ListField(type, root, entry, currentType.Name, segment);
            }

            if (field is ObjectField { ExecutionGroup: not null })
            {
                return ExecutionGroup_GroupRootField(type, root, entry, currentType.Name, segment);
            }

            if (field.Type.NamedType() is not ObjectType nextType)
            {
                return ExecutionGroup_NonObjectField(type, root, entry, currentType.Name, segment);
            }

            currentType = nextType;
        }

        return null;
    }
}
