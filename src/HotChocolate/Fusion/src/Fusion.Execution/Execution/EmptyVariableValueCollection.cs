using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// An <see cref="IVariableValueCollection"/> that never contains a variable.
/// </summary>
internal sealed class EmptyVariableValueCollection : IVariableValueCollection
{
    private EmptyVariableValueCollection()
    {
    }

    public static EmptyVariableValueCollection Instance { get; } = new();

    public bool IsEmpty => true;

    public T GetValue<T>(string name) where T : IValueNode
        => throw ThrowHelper.VariableNotFound(name);

    public bool TryGetValue<T>(string name, [NotNullWhen(true)] out T? value) where T : IValueNode
    {
        value = default;
        return false;
    }

    public IEnumerator<VariableValue> GetEnumerator()
        => Enumerable.Empty<VariableValue>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
