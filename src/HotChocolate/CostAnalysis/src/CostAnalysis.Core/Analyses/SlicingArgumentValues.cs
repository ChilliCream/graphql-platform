using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Provides slicing-argument values from field selections and schema defaults.
/// </summary>
internal static class SlicingArgumentValues
{
    private static readonly Dictionary<string, SlicingArgumentValue> s_empty = [];

    /// <summary>
    /// Pairs each slicing argument declared in <paramref name="metadata"/> with its supplied
    /// value and schema default. Returns an empty lookup when <paramref name="metadata"/>
    /// is <see langword="null"/> or declares no slicing arguments.
    /// </summary>
    public static IReadOnlyDictionary<string, SlicingArgumentValue> Build(
        ListSizeMetadata? metadata,
        IOutputFieldDefinition field,
        IReadOnlyList<ArgumentNode> arguments)
    {
        if (metadata is not { SlicingArguments.Length: > 0 })
        {
            return s_empty;
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
