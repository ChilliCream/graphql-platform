using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Adds evaluated policy condition variables to a coerced request variable collection.
/// </summary>
internal sealed class PolicyVariableValueCollection : IVariableValueCollection
{
    private const string Prefix = "__fusion_policy_";
    private readonly IVariableValueCollection _inner;
    private readonly int _slotCount;
    private ulong _liveFlags;
    private ulong _denyFlags;
    private ulong _fetchGateDenyFlags;

    public PolicyVariableValueCollection(
        IVariableValueCollection inner,
        int slotCount,
        ulong liveFlags,
        ulong denyFlags,
        ulong fetchGateDenyFlags)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegative(slotCount);

        _inner = inner;
        _slotCount = slotCount;
        _liveFlags = liveFlags;
        _denyFlags = denyFlags;
        _fetchGateDenyFlags = fetchGateDenyFlags;
    }

    public bool IsEmpty => _slotCount == 0 && _inner.IsEmpty;

    internal IVariableValueCollection Inner => _inner;

    internal ulong DenyFlags => _denyFlags;

    internal ulong LiveFlags => _liveFlags;

    internal ulong FetchGateDenyFlags => _fetchGateDenyFlags;

    internal void SetFlags(
        ulong liveFlags,
        ulong denyFlags,
        ulong fetchGateDenyFlags)
    {
        _liveFlags = liveFlags;
        _denyFlags = denyFlags;
        _fetchGateDenyFlags = fetchGateDenyFlags;
    }

    public T GetValue<T>(string name) where T : IValueNode
    {
        if (TryGetPolicyValue(name, out var value))
        {
            if (value is T casted)
            {
                return casted;
            }

            throw ThrowHelper.VariableNotOfType(name, typeof(T));
        }

        return _inner.GetValue<T>(name);
    }

    public bool TryGetValue<T>(string name, [NotNullWhen(true)] out T? value)
        where T : IValueNode
    {
        if (TryGetPolicyValue(name, out var policyValue))
        {
            if (policyValue is T casted)
            {
                value = casted;
                return true;
            }

            value = default;
            return false;
        }

        return _inner.TryGetValue(name, out value);
    }

    public IEnumerator<VariableValue> GetEnumerator() => _inner.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private bool TryGetPolicyValue(
        string name,
        [NotNullWhen(true)] out BooleanValueNode? value)
    {
        if (name.AsSpan().StartsWith(Prefix, StringComparison.Ordinal)
            && int.TryParse(name.AsSpan(Prefix.Length), out var ordinal)
            && (uint)ordinal < (uint)_slotCount)
        {
            var flag = 1UL << ordinal;
            value = (_liveFlags & flag) != 0 && (_fetchGateDenyFlags & flag) == 0
                ? BooleanValueNode.True
                : BooleanValueNode.False;
            return true;
        }

        value = null;
        return false;
    }
}
