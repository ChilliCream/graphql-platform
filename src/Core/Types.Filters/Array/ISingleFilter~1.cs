namespace HotChocolate.Types.Filters
{
    public interface ISingleFilter<out T> : ISingleFilter
    {
        T Element { get; }
    }
}
