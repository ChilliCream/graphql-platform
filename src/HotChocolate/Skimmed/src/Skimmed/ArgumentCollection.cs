using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a collection of argument values.
/// </summary>
public sealed class ArgumentCollection : IReadOnlyCollection<Argument>
{
    private readonly Dictionary<string, Argument> _arguments = new(StringComparer.Ordinal);
    private readonly IReadOnlyList<Argument> _order;

    public ArgumentCollection(IReadOnlyList<Argument> arguments)
    {
        foreach (var argument in arguments)
        {
            _arguments.Add(argument.Name, argument);
        }

        _order = arguments;
    }

    public int Count => _arguments.Count;

    public bool IsReadOnly => true;

    public IValueNode this[string argumentName]
    {
        get => _arguments[argumentName].Value;
    }

    public bool TryGetValue(string argumentName, [NotNullWhen(true)] out IValueNode? value)
    {
        if (_arguments.TryGetValue(argumentName, out var arg))
        {
            value = arg.Value;
            return true;
        }

        value = null;
        return false;
    }

    public IValueNode? GetValueOrDefault(string argumentName, IValueNode? defaultValue = null)
        => _arguments.TryGetValue(argumentName, out var value)
            ? value.Value
            : defaultValue;

    public bool ContainsName(string argumentName)
        => _arguments.ContainsKey(argumentName);

    public bool Contains(Argument argument)
        => _arguments.ContainsValue(argument);

    public void CopyTo(Argument[] array, int arrayIndex)
    {
        foreach (var argument in _order)
        {
            array[arrayIndex++] = argument;
        }
    }

    /// <inheritdoc />
    public IEnumerator<Argument> GetEnumerator()
        => _order.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

}
