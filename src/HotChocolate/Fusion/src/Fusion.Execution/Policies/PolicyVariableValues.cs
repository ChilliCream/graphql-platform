using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Policies;

/// <summary>
/// Computes the values of the synthetic policy variables of a variable set.
/// </summary>
internal static class PolicyVariableValues
{
    /// <summary>
    /// Adds a value for every variable to the variable value collection of the variable set.
    /// </summary>
    /// <param name="values">
    /// The variable value collection of the variable set.
    /// </param>
    /// <param name="variables">
    /// The synthetic variables of the operation and its incremental plans.
    /// </param>
    /// <param name="decisions">
    /// The denied selections of the variable set, or <c>null</c> if every selection is allowed.
    /// </param>
    /// <param name="booleanType">
    /// The schema's Boolean type.
    /// </param>
    public static void AddTo(
        VariableValueCollection values,
        ImmutableArray<PolicyVariable> variables,
        PolicyDecisions? decisions,
        IInputType booleanType)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(booleanType);

        var evaluated = new Dictionary<string, bool>(variables.Length, StringComparer.Ordinal);

        foreach (var variable in variables)
        {
            var value = Evaluate(variable, decisions, evaluated);

            values.Add(
                variable.Name,
                new VariableValue(
                    variable.Name,
                    booleanType,
                    value ? BooleanValueNode.True : BooleanValueNode.False));
        }
    }

    private static bool Evaluate(
        PolicyVariable variable,
        PolicyDecisions? decisions,
        Dictionary<string, bool> evaluated)
    {
        if (evaluated.TryGetValue(variable.Name, out var value))
        {
            return value;
        }

        if (variable.Operands.IsEmpty)
        {
            value = false;

            if (decisions is not null)
            {
                foreach (var selection in variable.Selections)
                {
                    if (decisions.IsDenied(selection))
                    {
                        value = true;
                        break;
                    }
                }
            }
        }
        else
        {
            value = true;

            foreach (var operand in variable.Operands)
            {
                if (!Evaluate(operand, decisions, evaluated))
                {
                    value = false;
                    break;
                }
            }
        }

        evaluated.Add(variable.Name, value);
        return value;
    }
}
