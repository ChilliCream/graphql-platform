using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class VariableValueCollection : IVariableValueCollection
{
    private readonly Dictionary<string, VariableValue> _coercedValues;

    public VariableValueCollection(Dictionary<string, VariableValue> coercedValues)
    {
        ArgumentNullException.ThrowIfNull(coercedValues);

        _coercedValues = coercedValues;
    }

    public static EmptyVariableValueCollection Empty => EmptyVariableValueCollection.Instance;

    /// <summary>
    /// Creates a collection with the values of <paramref name="values"/> and room for more values.
    /// </summary>
    /// <param name="values">
    /// The values to copy.
    /// </param>
    /// <param name="additionalCapacity">
    /// The number of values that can be added without growing the collection.
    /// </param>
    internal static VariableValueCollection Copy(IVariableValueCollection values, int additionalCapacity)
    {
        ArgumentNullException.ThrowIfNull(values);

        var count = 0;

        foreach (var _ in values)
        {
            count++;
        }

        var coercedValues = new Dictionary<string, VariableValue>(count + additionalCapacity);

        foreach (var value in values)
        {
            coercedValues[value.Name] = value;
        }

        return new VariableValueCollection(coercedValues);
    }

    public bool IsEmpty => _coercedValues.Count == 0;

    /// <summary>
    /// Adds a variable value to the collection.
    /// </summary>
    /// <param name="name">
    /// The variable name.
    /// </param>
    /// <param name="value">
    /// The variable value.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The collection already contains a variable with the name <paramref name="name"/>.
    /// </exception>
    internal void Add(string name, VariableValue value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (!_coercedValues.TryAdd(name, value))
        {
            throw ThrowHelper.VariableAlreadyExists(name);
        }
    }

    public T GetValue<T>(string name) where T : IValueNode
    {
        if (TryGetValue(name, out T? value))
        {
            return value;
        }

        if (_coercedValues.ContainsKey(name))
        {
            throw ThrowHelper.VariableNotOfType(name, typeof(T));
        }

        throw ThrowHelper.VariableNotFound(name);
    }

    public bool TryGetValue<T>(string name, [NotNullWhen(true)] out T? value) where T : IValueNode
    {
        if (_coercedValues.TryGetValue(name, out var variableValue)
            && variableValue.Value is T casted)
        {
            value = casted;
            return true;
        }

        value = default;
        return false;
    }

    public IEnumerator<VariableValue> GetEnumerator()
        => _coercedValues.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
