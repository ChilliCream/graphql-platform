namespace GreenDonut.Data.Expressions;

internal class Group<TKey, TValue>
{
    public TKey Key { get; set; } = default!;

    public List<TValue> Items { get; set; } = null!;
}
