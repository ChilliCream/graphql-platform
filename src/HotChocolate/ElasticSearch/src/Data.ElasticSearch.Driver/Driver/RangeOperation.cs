namespace HotChocolate.Data.ElasticSearch;

public class RangeOperation<T> : ILeafSearchOperation
{
    public RangeOperation(
        string path,
        int boost,
        ElasticSearchOperationKind kind)
    {
        Field = path;
        Boost = boost;
        Kind = kind;
    }

    public string Field { get; }

    public int Boost { get; }

    public RangeOperationValue<T>? GreaterThan { get; init; }

    public RangeOperationValue<T>? LowerThan { get; init; }

    public RangeOperationValue<T>? GreaterThanOrEquals { get; init; }

    public RangeOperationValue<T>? LowerThanOrEquals { get; init;}

    public ElasticSearchOperationKind Kind { get; }
}

public class RangeOperationValue<T>
{
    public RangeOperationValue(T value)
    {
        Value = value;
    }

    public T Value { get; }
}


