namespace HotChocolate.PersistedOperations.InMemory;

public class ServiceCollectionTests
{
    [Fact]
    public void AddFileSystemOperationDocumentStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateInMemoryPersistedOperationsServiceCollectionExtensions
                .AddInMemoryOperationDocumentStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>((Action)Action);
    }
}
