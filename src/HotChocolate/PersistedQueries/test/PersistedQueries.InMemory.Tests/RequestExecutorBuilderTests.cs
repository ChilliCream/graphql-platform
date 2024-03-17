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
                .AddInMemoryOperationDocumentStorage(null!);

        Assert.Throws<ArgumentNullException>(Action);
    }
}
