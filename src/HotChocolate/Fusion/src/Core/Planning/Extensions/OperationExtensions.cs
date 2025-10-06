using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Planning;

internal static class OperationExtensions
{
    public static ISelectionSet GetSelectionSet(this IOperation operation, ExecutionStep step)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (step == null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        if (step.ParentSelection == null)
        {
            if (step.SelectionSetType == operation.RootType)
            {
                return operation.RootSelectionSet;
            }

            throw new ArgumentException($"{nameof(step)}.ParentSelection is null.", nameof(step));
        }

        return operation.GetSelectionSet(step.ParentSelection, step.SelectionSetType);
    }

    public static IEnumerable<IObjectType> GetSchemaPossibleTypes(
        this IOperation operation,
        ISelection selection,
        FusionGraphConfiguration config,
        string schemaName)
    {
        var possibleTypes = new List<IObjectType>();

        foreach (var possibleType in operation.GetPossibleTypes(selection))
        {
            if (selection.Type.IsInterfaceType() || selection.Type.IsUnionType())
            {
                var declaringType = config.GetType<ObjectTypeMetadata>(possibleType.Name);

                // Due to a bug we currently do not properly annotate sources (bindings)
                // to types, so we can't directly check against the bindings of declaringType
                // and instead we have to look at the type's fields to determine if
                // it exists on the given subgraph.
                if (!declaringType.Fields.Any(field => HasFieldOnSubgraph(field, schemaName)))
                {
                    continue;
                }
            }

            possibleTypes.Add(possibleType);
        }

        return possibleTypes;
    }

    private static bool HasFieldOnSubgraph(ObjectFieldInfo field, string schemaName)
    {
        return !field.Name.EqualsOrdinal(IntrospectionFields.TypeName) && field.Bindings.ContainsSubgraph(schemaName);
    }
}
