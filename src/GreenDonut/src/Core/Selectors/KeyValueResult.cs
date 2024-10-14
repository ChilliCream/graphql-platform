namespace GreenDonut.Selectors;

/// <summary>
/// This class is a helper that is used to project a key value pair.
/// </summary>
public sealed class KeyValueResult<TKey, TValue>
{
    public TKey Key { get; set; } = default!;

    public TValue Value { get; set; } = default!;
}
