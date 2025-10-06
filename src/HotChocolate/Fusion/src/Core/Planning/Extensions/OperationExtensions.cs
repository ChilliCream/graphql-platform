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

    public static IEnumerable<IObjectType> GetSchemaPossibleTypes(this IOperation operation, ISelection selection, FusionGraphConfiguration config, string subgraph)
    {
        var possibleTypes = new List<IObjectType>();

        foreach (var possibleType in operation.GetPossibleTypes(selection))
        {
            if (!string.IsNullOrWhiteSpace(subgraph) && (selection.Type.IsInterfaceType() || selection.Type.IsUnionType()))
            {
                var declaringType = config.GetType<ObjectTypeMetadata>(possibleType.Name);
                if (!declaringType.Fields.Any(
                    field =>
                    // We exclude the typename because it's present in all subgraphs
                    !field.Name.EqualsOrdinal(IntrospectionFields.TypeName)
                    && field.Bindings.ContainsSubgraph(subgraph)))
                {
                    // The current graph can't resolve this type
                    continue;
                }
            }

            possibleTypes.Add(possibleType);
        }

        return possibleTypes;
    }
}
