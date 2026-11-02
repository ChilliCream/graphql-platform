using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

internal sealed class FieldSemanticIdentityComparer : IEqualityComparer<FieldSemanticIdentity>
{
    public static FieldSemanticIdentityComparer Instance { get; } = new();

    public bool Equals(FieldSemanticIdentity x, FieldSemanticIdentity y)
    {
        if (!x.OutputType.Equals(y.OutputType, TypeComparison.Structural)
            || x.FieldWeight != y.FieldWeight
            || x.ReturnTypeWeight != y.ReturnTypeWeight
            || !Equals(x.ListSize, y.ListSize)
            || x.Arguments.Count != y.Arguments.Count)
        {
            return false;
        }

        for (var i = 0; i < x.Arguments.Count; i++)
        {
            var left = x.Arguments[i];
            var right = y.Arguments[i];

            if (!string.Equals(left.Name, right.Name, StringComparison.Ordinal)
                || BitConverter.DoubleToInt64Bits(left.Weight)
                    != BitConverter.DoubleToInt64Bits(right.Weight)
                || !string.Equals(left.TypeName, right.TypeName, StringComparison.Ordinal)
                || !SyntaxEquals(left.DefaultValue, right.DefaultValue))
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(FieldSemanticIdentity value)
    {
        var hash = new HashCode();
        var outputType = value.OutputType;

        while (outputType is IWrapperType wrapper)
        {
            hash.Add(outputType.Kind);
            outputType = wrapper.InnerType;
        }

        hash.Add(outputType.Kind);
        hash.Add(outputType.NamedType().Name, StringComparer.Ordinal);
        hash.Add(value.FieldWeight);
        hash.Add(value.ReturnTypeWeight);
        AddListSize(ref hash, value.ListSize);

        foreach (var argument in value.Arguments)
        {
            hash.Add(argument.Name, StringComparer.Ordinal);
            hash.Add(BitConverter.DoubleToInt64Bits(argument.Weight));
            hash.Add(argument.TypeName, StringComparer.Ordinal);

            if (argument.DefaultValue is { } defaultValue)
            {
                hash.Add(SyntaxComparer.BySyntax.GetHashCode(defaultValue));
            }
        }

        return hash.ToHashCode();
    }

    private static bool Equals(ListSizeMetadata? left, ListSizeMetadata? right)
    {
        if (left is null || right is null)
        {
            return ReferenceEquals(left, right);
        }

        return NullableBitsEqual(left.AssumedSize, right.AssumedSize)
            && NullableBitsEqual(left.SlicingArgumentDefaultValue, right.SlicingArgumentDefaultValue)
            && left.RequireOneSlicingArgument == right.RequireOneSlicingArgument
            && SequenceEqual(left.SlicingArguments, right.SlicingArguments)
            && SequenceEqual(left.SizedFields, right.SizedFields);
    }

    private static void AddListSize(ref HashCode hash, ListSizeMetadata? metadata)
    {
        if (metadata is null)
        {
            hash.Add(false);
            return;
        }

        hash.Add(true);
        AddNullableDouble(ref hash, metadata.AssumedSize);
        AddNullableDouble(ref hash, metadata.SlicingArgumentDefaultValue);
        hash.Add(metadata.RequireOneSlicingArgument);

        foreach (var name in metadata.SlicingArguments)
        {
            hash.Add(name, StringComparer.Ordinal);
        }

        foreach (var name in metadata.SizedFields)
        {
            hash.Add(name, StringComparer.Ordinal);
        }
    }

    private static void AddNullableDouble(ref HashCode hash, double? value)
    {
        hash.Add(value.HasValue);
        hash.Add(value is { } number ? BitConverter.DoubleToInt64Bits(number) : 0);
    }

    private static bool NullableBitsEqual(double? left, double? right)
        => left.HasValue == right.HasValue
            && (!left.HasValue
                || BitConverter.DoubleToInt64Bits(left.GetValueOrDefault())
                    == BitConverter.DoubleToInt64Bits(right.GetValueOrDefault()));

    private static bool SequenceEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SyntaxEquals(IValueNode? left, IValueNode? right)
        => left is null || right is null
            ? ReferenceEquals(left, right)
            : SyntaxComparer.BySyntax.Equals(left, right);
}
