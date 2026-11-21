using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

/// <summary>
/// Base class for formatters that serialize an <see cref="OperationPlan"/>
/// into a human- or machine-readable string representation.
/// </summary>
public abstract class OperationPlanFormatter
{
    /// <summary>
    /// Formats the specified <paramref name="plan"/> as a string.
    /// </summary>
    /// <param name="plan">The operation plan to format.</param>
    /// <param name="trace">Optional trace information to include in the output.</param>
    /// <returns>A string representation of the operation plan.</returns>
    public abstract string Format(OperationPlan plan, OperationPlanTrace? trace = null);

    private protected static int GetIncrementalPlanIndex(OperationPlan plan, Operation operation)
    {
        for (var i = 0; i < plan.IncrementalPlans.Length; i++)
        {
            if (ReferenceEquals(plan.IncrementalPlans[i].Operation, operation))
            {
                return i;
            }
        }

        throw ThrowHelper.PolicyPlanSelectionNotInPlan();
    }

    private protected static SelectionPath GetSelectionPath(Selection selection)
    {
        // Collected from the selection up to the root and replayed in reverse.
        var segments = new List<(string Name, bool IsTypeCondition)>();
        var current = selection;

        while (true)
        {
            segments.Add((current.ResponseName, false));

            var selectionSet = current.DeclaringSelectionSet;

            if (selectionSet.DeclaringSelection is not { } parent)
            {
                break;
            }

            if (parent.NamedType is not IObjectTypeDefinition)
            {
                segments.Add((selectionSet.Type.Name, true));
            }

            current = parent;
        }

        var path = SelectionPath.Root;

        for (var i = segments.Count - 1; i >= 0; i--)
        {
            var (name, isTypeCondition) = segments[i];
            path = isTypeCondition ? path.AppendFragment(name) : path.AppendField(name);
        }

        return path;
    }
}
