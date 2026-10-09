using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Resolves list sizes in this order: inherited <c>sizedFields</c> sizes, supplied slicing
/// arguments, <c>slicingArgumentDefaultValue</c>, <c>assumedSize</c>, then
/// <see cref="CostSchemaIndexOptions.DefaultListSize"/>.
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
    /// The sizes inherited from parent types whose <c>sizedFields</c> names this field.
    /// Empty when no parent provides a size.
    /// </param>
    /// <param name="slicingArguments">
    /// This field call's slicing arguments, keyed by the argument name as
    /// declared in <see cref="ListSizeMetadata.SlicingArguments"/>. An
    /// argument name absent from this dictionary is treated as carrying no
    /// supplied value and no schema default.
    /// </param>
    /// <param name="variableValues">
    /// Resolves a slicing argument's coerced variable value, or
    /// <see langword="null"/> for the assumed bound, where a variable-bound
    /// slicing argument reads its <c>assumedSize</c>, else the default list
    /// size, instead of being coerced.
    /// </param>
    /// <param name="defaultListSize">
    /// The fallback list size.
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

    /// <summary>
    /// Resolves the size a field's own <c>@listSize(sizedFields:)</c>
    /// propagates to its named child fields.
    /// </summary>
    /// <param name="metadata">
    /// The field's own <c>@listSize</c> metadata, or <see langword="null"/>
    /// when the field carries no usage. Returns <see langword="false"/> when
    /// <see langword="null"/> or when <see cref="ListSizeMetadata.SizedFields"/>
    /// is empty.
    /// </param>
    /// <param name="slicingArguments">
    /// This field call's slicing arguments, keyed by the argument name as
    /// declared in <see cref="ListSizeMetadata.SlicingArguments"/>. An
    /// argument name absent from this dictionary is treated as carrying no
    /// supplied value and no schema default.
    /// </param>
    /// <param name="variableValues">
    /// Resolves a slicing argument's coerced variable value, or
    /// <see langword="null"/> for the assumed bound, where a variable-bound
    /// slicing argument reads <see cref="ListSizeMetadata.AssumedSize"/> then
    /// <paramref name="defaultListSize"/>, matching <see cref="Resolve"/>.
    /// </param>
    /// <param name="defaultListSize">
    /// The fallback list size when a variable argument has no supplied value or assumed size.
    /// </param>
    /// <param name="size">
    /// The resolved size, clamped to 0 when negative. Undefined when this
    /// method returns <see langword="false"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a size was resolved from a present
    /// slicing argument, <see cref="ListSizeMetadata.SlicingArgumentDefaultValue"/>
    /// or <see cref="ListSizeMetadata.AssumedSize"/>; otherwise
    /// <see langword="false"/>.
    /// </returns>
    public static bool TryResolveSizedFieldSize(
        ListSizeMetadata? metadata,
        IReadOnlyDictionary<string, SlicingArgumentValue> slicingArguments,
        ICostVariableValues? variableValues,
        double defaultListSize,
        out double size)
    {
        if (metadata is null || metadata.SizedFields.Length == 0)
        {
            size = 0.0;
            return false;
        }

        var staticFallback = metadata.AssumedSize ?? defaultListSize;

        if (TryResolveSlicingArgumentValue(
                metadata.SlicingArguments,
                slicingArguments,
                variableValues,
                staticFallback,
                out var slicingValue))
        {
            size = Clamp0(slicingValue);
            return true;
        }

        if (metadata.SlicingArgumentDefaultValue is { } slicingArgumentDefaultValue)
        {
            size = Clamp0(slicingArgumentDefaultValue);
            return true;
        }

        if (metadata.AssumedSize is { } assumedSize)
        {
            size = assumedSize;
            return true;
        }

        size = 0.0;
        return false;
    }

    private static bool TryResolveSlicingArgumentValue(
        ImmutableArray<string> slicingArgumentNames,
        IReadOnlyDictionary<string, SlicingArgumentValue> slicingArguments,
        ICostVariableValues? variableValues,
        double? staticFallback,
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
    /// Gets a slicing argument's numeric value. Omitted arguments and undefined variables
    /// use the schema default; explicit null values do not. When
    /// <paramref name="variableValues"/> is <see langword="null"/>, variable arguments use
    /// <paramref name="staticFallback"/> and are absent if that fallback is <see langword="null"/>.
    /// </summary>
    private static bool TryResolveArgumentValue(
        SlicingArgumentValue argument,
        ICostVariableValues? variableValues,
        double? staticFallback,
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
                if (staticFallback is not { } fallback)
                {
                    value = 0.0;
                    return false;
                }

                value = fallback;
                return true;
            }

            effective = variableValues.TryGetValue(variable.Name.Value, out var coerced)
                ? coerced
                : argument.SchemaDefaultValue;
        }

        return TryReadNumber(effective, out value);
    }

    /// <summary>
    /// Reads an integer slicing value. Returns <see langword="false"/> for null or non-integer values.
    /// </summary>
    private static bool TryReadNumber(IValueNode? value, out double result)
    {
        if (value is IntValueNode intValue)
        {
            result = intValue.ToDouble();
            return true;
        }

        result = 0.0;
        return false;
    }

    private static double Clamp0(double value) => value < 0.0 ? 0.0 : value;
}
