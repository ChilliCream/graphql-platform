namespace HotChocolate.PersistedQueries.InMemory;

public class ServiceCollectionTests
{
    [Fact]
    public void AddFileSystemQueryStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateInMemoryPersistedQueriesServiceCollectionExtensions
                .AddInMemoryOperationDocumentStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>((Action)Action);
    }
}
    
