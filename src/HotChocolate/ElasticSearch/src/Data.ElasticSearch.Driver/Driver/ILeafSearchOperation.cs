namespace HotChocolate.Data.ElasticSearch;

public interface ILeafSearchOperation : ISearchOperation
{
    ElasticSearchOperationKind Kind { get; }

    string Field { get; }

    int Boost { get; }
}
