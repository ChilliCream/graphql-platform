using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.PersistedQueries.FileSystem;

public class RequestExecutorBuilderTests
{
    [Fact]
    public void AddFileSystemQueryStorage_2_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateFileSystemPersistedQueriesRequestExecutorBuilderExtensions
                .AddFileSystemQueryStorage(null!);

        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddReadOnlyFileSystemQueryStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateFileSystemPersistedQueriesRequestExecutorBuilderExtensions
                .AddReadOnlyFileSystemQueryStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }
}
