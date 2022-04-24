namespace HotChocolate.Data.ElasticSearch;

public interface ILeafSearchOperation : ISearchOperation
{
    ElasticSearchOperationKind Kind { get; }
}
