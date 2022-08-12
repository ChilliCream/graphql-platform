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
                .AddInMemoryQueryStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>((Action) Action);
    }

    [Fact]
    public void AddReadOnlyFileSystemQueryStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateInMemoryPersistedQueriesServiceCollectionExtensions
                .AddReadOnlyInMemoryQueryStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }
}
