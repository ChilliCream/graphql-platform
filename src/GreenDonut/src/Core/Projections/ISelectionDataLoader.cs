#if NET8_0_OR_GREATER

namespace GreenDonut.Projections;

public interface ISelectionDataLoader<in TKey, TValue>
    : IDataLoader<TKey, TValue> where TKey : notnull
{
    IDataLoader<TKey, TValue> Root { get; }
}
#endif
