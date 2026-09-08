using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One slicing argument's supplied and schema-default values, the inputs
/// <see cref="ListSizeResolver"/> needs to resolve that argument after
/// coercion.
/// </summary>
/// <param name="SuppliedValue">
/// The value supplied for this argument in the operation, or
/// <see langword="null"/> when the argument was omitted.
/// </param>
/// <param name="SchemaDefaultValue">
/// The argument's schema-declared default value, or <see langword="null"/>
/// when it has none.
/// </param>
internal readonly record struct SlicingArgumentValue(IValueNode? SuppliedValue, IValueNode? SchemaDefaultValue);

/// <summary>
/// Resolves the list multiplier for one field call from the locked
/// priority chain: inherited <c>sizedFields</c>, slicing arguments present
/// after coercion, <c>slicingArgumentDefaultValue</c>, <c>assumedSize</c>,
/// then <see cref="CostEngineOptions.DefaultListSize"/>.
/// </summary>
internal static class ListSizeResolver
{
    /// <summary>
    /// Resolves the list multiplier for a field call.
    /// </summary>
    /// <param name="isListField">
    /// Whether the field's own return type is a list. A non-list field
    /// always resolves to 1.0.
    /// </param>
    /// <param name="metadata">
    /// The field's own <c>@listSize</c> metadata, or <see langword="null"/>
    /// when the field carries no usage. An annotation whose
    /// <c>sizedFields</c> is non-empty never sizes its own field.
    /// </param>
    /// <param name="inheritedSizes">
    /// The sizes inherited from every possible parent object type whose
    /// <c>sizedFields</c> names this field, empty when none do.
    /// </param>
    /// <param name="slicingArguments">
    /// This field call's slicing arguments, keyed by the argument name as
    /// declared in <see cref="ListSizeMetadata.SlicingArguments"/>. An
    /// argument name absent from this dictionary is treated as carrying no
    /// supplied value and no schema default.
    /// </param>
    /// <param name="variableValues">
    /// Resolves a slicing argument's coerced variable value, or
    /// <see langword="null"/> for the static bound, where a variable-bound
    /// slicing argument reads its <c>assumedSize</c>, else the default list
    /// size, instead of being coerced.
    /// </param>
    /// <param name="defaultListSize">
    /// The engine's fallback list size.
    /// </param>
    /// <returns>
    /// The list multiplier, 1.0 for a non-list field.
    /// </returns>
    public static double Resolve(
        bool isListField,
        ListSizeMetadata? metadata,
        ReadOnlySpan<double> inheritedSizes,
        IReadOnlyDictionary<string, SlicingArgumentValue> slicingArguments,
        ICostVariableValues? variableValues,
        double defaultListSize)
    {
        if (!isListField)
        {
            return 1.0;
        }

        if (inheritedSizes.Length > 0)
        {
            var maxInherited = inheritedSizes[0];

            for (var i = 1; i < inheritedSizes.Length; i++)
            {
                if (inheritedSizes[i] > maxInherited)
                {
                    maxInherited = inheritedSizes[i];
                }
            }

            return maxInherited;
        }

        if (metadata is null)
        {
            return defaultListSize;
        }

        if (metadata.SizedFields.Length > 0)
        {
            return defaultListSize;
        }

        var staticFallback = metadata.AssumedSize ?? defaultListSize;

        if (TryResolveSlicingArgumentValue(
                metadata.SlicingArguments,
                slicingArguments,
                variableValues,
                staticFallback,
                out var slicingValue))
        {
            return Clamp0(slicingValue);
        }

        if (metadata.SlicingArgumentDefaultValue is { } slicingArgumentDefaultValue)
        {
            return Clamp0(slicingArgumentDefaultValue);
        }

        if (metadata.AssumedSize is { } assumedSize)
        {
            return assumedSize;
        }

        return defaultListSize;
    }

    private static bool TryResolveSlicingArgumentValue(
        ImmutableArray<string> slicingArgumentNames,
        IReadOnlyDictionary<string, SlicingArgumentValue> slicingArguments,
        ICostVariableValues? variableValues,
        double staticFallback,
        out double value)
    {
        var found = false;
        var max = 0.0;

        foreach (var name in slicingArgumentNames)
        {
            if (!slicingArguments.TryGetValue(name, out var argument)
                || !TryResolveArgumentValue(argument, variableValues, staticFallback, out var argumentValue))
            {
                continue;
            }

            if (!found || argumentValue > max)
            {
                max = argumentValue;
            }

            found = true;
        }

        value = max;
        return found;
    }

    /// <summary>
    /// Resolves one slicing argument's numeric value after coercion. An
    /// omitted argument or an undefined variable falls back to the schema
    /// default; an explicit null, literal or variable-bound, is present but
    /// not numeric and so suppresses that fallback (R-NULL-VARIABLE). A
    /// variable-bound slicing argument reads <paramref name="staticFallback"/>
    /// on the static path, where <paramref name="variableValues"/> is
    /// <see langword="null"/>, instead of the schema default.
    /// </summary>
    private static bool TryResolveArgumentValue(
        SlicingArgumentValue argument,
        ICostVariableValues? variableValues,
        double staticFallback,
        out double value)
    {
        var effective = argument.SuppliedValue;

        if (effective is null)
        {
            effective = argument.SchemaDefaultValue;
        }
        else if (effective is VariableNode variable)
        {
            if (variableValues is null)
            {
                value = staticFallback;
                return true;
            }

            effective = variableValues.TryGetValue(variable.Name.Value, out var coerced)
                ? coerced
                : argument.SchemaDefaultValue;
        }

        return TryReadNumber(effective, out value);
    }

    private static bool TryReadNumber(IValueNode? value, out double result)
    {
        switch (value)
        {
            case IntValueNode intValue:
                result = intValue.ToDouble();
                return true;

            case FloatValueNode floatValue:
                result = floatValue.ToDouble();
                return true;

            default:
                result = 0.0;
                return false;
        }
    }

    private static double Clamp0(double value) => value < 0.0 ? 0.0 : value;
}
