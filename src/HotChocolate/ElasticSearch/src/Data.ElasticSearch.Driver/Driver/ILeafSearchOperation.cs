namespace HotChocolate.Data.ElasticSearch;

public interface ILeafSearchOperation : ISearchOperation
{
    string Path { get; }

    ElasticSearchOperationKind Kind { get; }
}
