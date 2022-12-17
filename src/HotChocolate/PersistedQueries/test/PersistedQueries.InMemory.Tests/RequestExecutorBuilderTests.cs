using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.PersistedQueries.InMemory;

public class RequestExecutorBuilderTests
{
    [Fact]
    public void AddFileSystemQueryStorage_2_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateInMemoryPersistedQueriesRequestExecutorBuilderExtensions
                .AddInMemoryQueryStorage(null!);

        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddReadOnlyFileSystemQueryStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateInMemoryPersistedQueriesRequestExecutorBuilderExtensions
                .AddReadOnlyInMemoryQueryStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }
}
