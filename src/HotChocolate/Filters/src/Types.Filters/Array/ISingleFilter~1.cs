namespace HotChocolate.Types.Filters
{
    public interface ISingleFilter<out T> : ISingleFilter
    {
        [FilterMetaField]
        T Element { get; }
    }
}
