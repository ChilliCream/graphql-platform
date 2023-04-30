namespace HotChocolate.Data.ElasticSearch;

public class RangeOperationValue<T>
{
    public RangeOperationValue(T value)
    {
        Value = value;
    }

    public T Value { get; }
}
