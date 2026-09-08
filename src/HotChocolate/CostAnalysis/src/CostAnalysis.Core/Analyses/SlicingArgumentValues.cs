using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Builds the slicing-argument lookup <see cref="ListSizeResolver"/> needs
/// from a field's <c>@listSize</c> metadata and its call's literal
/// arguments, shared by <see cref="CostAlgebra"/> and the ExactCases
/// traversal.
/// </summary>
internal static class SlicingArgumentValues
{
    private static readonly Dictionary<string, SlicingArgumentValue> Empty = [];

    /// <summary>
    /// Builds one slicing-argument entry per name declared in
    /// <paramref name="metadata"/>'s <c>slicingArguments</c>, pairing its
    /// literal supplied value with its schema default; an empty lookup when
    /// <paramref name="metadata"/> is <see langword="null"/> or declares no
    /// slicing arguments.
    /// </summary>
    public static IReadOnlyDictionary<string, SlicingArgumentValue> Build(
        ListSizeMetadata? metadata,
        IOutputFieldDefinition field,
        IReadOnlyList<ArgumentNode> arguments)
    {
        if (metadata is not { SlicingArguments.Length: > 0 })
        {
            return Empty;
        }

        var result = new Dictionary<string, SlicingArgumentValue>(metadata.SlicingArguments.Length);

        foreach (var name in metadata.SlicingArguments)
        {
            var schemaDefault = field.Arguments.TryGetField(name, out var argumentDefinition)
                ? argumentDefinition.DefaultValue
                : null;
            result[name] = new SlicingArgumentValue(FindArgumentValue(arguments, name), schemaDefault);
        }

        return result;
    }

    /// <summary>
    /// Finds the literal value supplied for <paramref name="name"/> among
    /// <paramref name="arguments"/>, or <see langword="null"/> when absent.
    /// </summary>
    public static IValueNode? FindArgumentValue(IReadOnlyList<ArgumentNode> arguments, string name)
    {
        foreach (var argument in arguments)
        {
            if (string.Equals(argument.Name.Value, name, StringComparison.Ordinal))
            {
                return argument.Value;
            }
        }

        return null;
    }
}
