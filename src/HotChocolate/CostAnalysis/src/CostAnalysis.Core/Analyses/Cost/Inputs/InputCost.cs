using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Computes the cost of supplied input values from snapshot metadata.
/// </summary>
internal static class InputCost
{
    /// <summary>
    /// Computes one argument or input field's own weight and nested value cost.
    /// </summary>
    public static double Compute(
        CostSchemaSnapshot snapshot,
        InputValueMetadata definition,
        IValueNode? suppliedValue,
        ICostVariableValues? variableValues)
    {
        if (!TryResolveValue(definition, suppliedValue, variableValues, out var value, out var staticShape))
        {
            return 0.0;
        }

        return definition.Weight + (staticShape
            ? ComputeStaticShape(snapshot, definition.TypeName, [])
            : ComputeValue(snapshot, definition.TypeName, value!, variableValues));
    }

    private static bool TryResolveValue(
        InputValueMetadata definition,
        IValueNode? suppliedValue,
        ICostVariableValues? variableValues,
        out IValueNode? value,
        out bool staticShape)
    {
        staticShape = false;

        if (suppliedValue is not VariableNode variable)
        {
            value = suppliedValue ?? definition.DefaultValue;
            return value is not null;
        }

        if (variableValues is null)
        {
            value = null;
            staticShape = true;
            return true;
        }

        if (variableValues.TryGetValue(variable.Name.Value, out value))
        {
            return true;
        }

        value = definition.DefaultValue;
        return value is not null;
    }

    private static double ComputeValue(
        CostSchemaSnapshot snapshot,
        string typeName,
        IValueNode value,
        ICostVariableValues? variableValues)
    {
        if (value is NullValueNode)
        {
            return 0.0;
        }

        if (value is ListValueNode list)
        {
            var cost = 0.0;

            foreach (var item in list.Items)
            {
                cost += ComputeValue(snapshot, typeName, item, variableValues);
            }

            return cost;
        }

        if (value is ObjectValueNode inputObject
            && snapshot.TryGetInputObjectFields(typeName, out var fields))
        {
            var cost = 0.0;

            foreach (var field in fields)
            {
                cost += Compute(snapshot, field, FindFieldValue(inputObject, field.Name), variableValues);
            }

            return cost;
        }

        return 0.0;
    }

    private static double ComputeStaticShape(
        CostSchemaSnapshot snapshot,
        string typeName,
        HashSet<string> ancestors)
    {
        if (!snapshot.TryGetInputObjectFields(typeName, out var fields) || !ancestors.Add(typeName))
        {
            return 0.0;
        }

        var cost = 0.0;

        foreach (var field in fields)
        {
            cost += field.Weight + ComputeStaticShape(snapshot, field.TypeName, ancestors);
        }

        ancestors.Remove(typeName);
        return cost;
    }

    private static IValueNode? FindFieldValue(ObjectValueNode inputObject, string fieldName)
    {
        foreach (var field in inputObject.Fields)
        {
            if (string.Equals(field.Name.Value, fieldName, StringComparison.Ordinal))
            {
                return field.Value;
            }
        }

        return null;
    }
}
