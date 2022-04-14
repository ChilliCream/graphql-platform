using System.Collections.Generic;

namespace HotChocolate.Data.ElasticSearch;

public class QueryDefinition
{
    public QueryDefinition(
        IReadOnlyList<ISearchOperation> query,
        IReadOnlyList<ISearchOperation> filter)
    {
        Query = query;
        Filter = filter;
    }

    public IReadOnlyList<ISearchOperation> Query { get; }

    public IReadOnlyList<ISearchOperation> Filter { get; }
}

public enum ElasticSearchOperationKind
{
    Filter,
    Query
}

#pragma warning disable CA1040
public interface ISearchOperation
{
}
#pragma warning restore CA1040

public class BoolOperation : ISearchOperation
{
    public BoolOperation(
        IReadOnlyList<ISearchOperation> must,
        IReadOnlyList<ISearchOperation> should)
    {
        Must = must;
        Should = should;
    }

    public IReadOnlyList<ISearchOperation> Must { get; }

    public IReadOnlyList<ISearchOperation> Should { get; }

}

public interface ILeafSearchOperation : ISearchOperation
{
    string Path { get; }

    ElasticSearchOperationKind Kind { get; }
}

public class MatchOperation : ILeafSearchOperation
{
    public MatchOperation(string path, ElasticSearchOperationKind kind, string? value)
    {
        Path = path;
        Value = value;
        Kind = kind;
    }

    public string Path { get; }

    public string? Value { get; }

    public ElasticSearchOperationKind Kind { get; }
}

public class TermOperation : ILeafSearchOperation
{
    public TermOperation(
        string path,
        ElasticSearchOperationKind kind,
        object value)
    {
        Path = path;
        Value = value;
        Kind = kind;
    }

    public string Path { get; }

    public object Value { get; }

    public ElasticSearchOperationKind Kind { get; }
}

public class RangeOperation : ILeafSearchOperation
{
    public RangeOperation(
        string path,
        ElasticSearchOperationKind kind,
        double? greaterThan,
        double? lowerThan,
        double? greaterThanOrEquals,
        double? lowerThanOrEquals)
    {
        Path = path;
        GreaterThan = greaterThan;
        LowerThan = lowerThan;
        GreaterThanOrEquals = greaterThanOrEquals;
        LowerThanOrEquals = lowerThanOrEquals;
        Kind = kind;
    }

    public string Path { get; }

    public double? GreaterThan { get; }

    public double? LowerThan { get; }

    public double? GreaterThanOrEquals { get; }

    public double? LowerThanOrEquals { get; }

    public ElasticSearchOperationKind Kind { get; }
}


